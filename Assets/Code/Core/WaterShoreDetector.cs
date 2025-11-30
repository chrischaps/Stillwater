using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Stillwater.Core
{
    /// <summary>
    /// Detects shore edges where water tiles meet non-water tiles and generates
    /// a mask texture for the water shader to use for foam effects.
    /// </summary>
    [ExecuteAlways]
    public class WaterShoreDetector : MonoBehaviour
    {
        [Header("Tilemap References")]
        [Tooltip("The water tilemap to analyze")]
        [SerializeField] private Tilemap waterTilemap;

        [Tooltip("Optional: Ground tilemap to check against. If not set, any non-water tile counts as shore.")]
        [SerializeField] private Tilemap groundTilemap;

        [Header("Mask Settings")]
        [Tooltip("Resolution multiplier for the shore mask (pixels per tile)")]
        [SerializeField] private int pixelsPerTile = 16;

        [Tooltip("How wide the foam band is (smaller = thinner foam closer to edge)")]
        [SerializeField] [Range(0.05f, 0.5f)] private float shoreWidth = 0.15f;

        [Tooltip("Softness of the shore edge falloff")]
        [SerializeField] [Range(0f, 1f)] private float shoreSoftness = 0.3f;

        [Tooltip("How much noise to add to the shore edge (0 = smooth, 1 = very wavy)")]
        [SerializeField] [Range(0f, 1f)] private float shoreNoise = 0.4f;

        [Tooltip("Scale of the noise pattern (smaller = larger waves)")]
        [SerializeField] [Range(0.5f, 5f)] private float noiseScale = 2f;

        [Header("Material")]
        [Tooltip("The water material to apply the shore mask to")]
        [SerializeField] private Material waterMaterial;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        [SerializeField] private bool regenerateOnUpdate = false;
        [SerializeField] private bool logDebugInfo = true;

        [Header("Generated Mask Preview")]
        [SerializeField] private Texture2D debugMaskPreview;

        private RenderTexture shoreMaskTexture;
        private Texture2D shoreMaskCPU;
        private Bounds tilemapBounds;
        private bool isDirty = true;

        // Shader property IDs
        private static readonly int ShoreMaskTexID = Shader.PropertyToID("_ShoreMaskTex");
        private static readonly int ShoreMaskBoundsMinID = Shader.PropertyToID("_ShoreMaskBoundsMin");
        private static readonly int ShoreMaskBoundsSizeID = Shader.PropertyToID("_ShoreMaskBoundsSize");
        private static readonly int UseShoreDetectionID = Shader.PropertyToID("_UseShoreDetection");

        private void OnEnable()
        {
            isDirty = true;
        }

        private void OnDisable()
        {
            CleanupTextures();
            if (waterMaterial != null)
            {
                waterMaterial.SetFloat(UseShoreDetectionID, 0f);
            }
        }

        private void Update()
        {
            if (isDirty || regenerateOnUpdate)
            {
                GenerateShoreMask();
                isDirty = false;
            }
        }

        /// <summary>
        /// Call this to regenerate the shore mask (e.g., after tilemap changes)
        /// </summary>
        public void MarkDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// Force immediate regeneration of the shore mask
        /// </summary>
        [ContextMenu("Regenerate Shore Mask")]
        public void GenerateShoreMask()
        {
            if (waterTilemap == null)
            {
                Debug.LogWarning("WaterShoreDetector: No water tilemap assigned!");
                return;
            }

            // Get tilemap bounds
            waterTilemap.CompressBounds();
            BoundsInt cellBounds = waterTilemap.cellBounds;

            if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0)
            {
                Debug.LogWarning("WaterShoreDetector: Water tilemap is empty!");
                return;
            }

            // For isometric tilemaps, we need to calculate actual world bounds
            // by checking all corners of all cells
            Vector3 minWorld = Vector3.positiveInfinity;
            Vector3 maxWorld = Vector3.negativeInfinity;

            foreach (var cellPos in cellBounds.allPositionsWithin)
            {
                if (!IsWaterTile(cellPos)) continue;

                // Get the center of this cell in world space
                Vector3 cellCenter = waterTilemap.GetCellCenterWorld(cellPos);

                // Also check the cell corners for accurate bounds
                Vector3 cellWorld = waterTilemap.CellToWorld(cellPos);
                Vector3 cellSize = waterTilemap.cellSize;

                // For isometric, expand bounds based on cell center
                minWorld = Vector3.Min(minWorld, cellCenter - cellSize);
                maxWorld = Vector3.Max(maxWorld, cellCenter + cellSize);
            }

            // Fallback if no water tiles found
            if (minWorld.x == float.PositiveInfinity)
            {
                minWorld = waterTilemap.CellToWorld(cellBounds.min);
                maxWorld = waterTilemap.CellToWorld(cellBounds.max);
            }

            tilemapBounds = new Bounds();
            tilemapBounds.SetMinMax(minWorld, maxWorld);

            // Add some padding
            tilemapBounds.Expand(2f);

            // Create texture
            int texWidth = Mathf.Max(1, cellBounds.size.x * pixelsPerTile);
            int texHeight = Mathf.Max(1, cellBounds.size.y * pixelsPerTile);

            // Clamp to reasonable size
            texWidth = Mathf.Min(texWidth, 2048);
            texHeight = Mathf.Min(texHeight, 2048);

            // Create CPU texture for writing
            if (shoreMaskCPU == null || shoreMaskCPU.width != texWidth || shoreMaskCPU.height != texHeight)
            {
                if (shoreMaskCPU != null)
                {
                    DestroyImmediate(shoreMaskCPU);
                }
                shoreMaskCPU = new Texture2D(texWidth, texHeight, TextureFormat.R8, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            // Generate the shore mask by iterating through cells directly
            Color[] pixels = new Color[texWidth * texHeight];

            // Initialize all pixels to black (no shore)
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }

            // Neighbor offsets for checking adjacent cells
            Vector3Int[] neighborOffsets = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),   // Right
                new Vector3Int(-1, 0, 0),  // Left
                new Vector3Int(0, 1, 0),   // Up
                new Vector3Int(0, -1, 0),  // Down
                new Vector3Int(1, 1, 0),   // Up-Right
                new Vector3Int(-1, 1, 0),  // Up-Left
                new Vector3Int(1, -1, 0),  // Down-Right
                new Vector3Int(-1, -1, 0)  // Down-Left
            };

            // Build a set of shore cells and their non-water neighbor directions
            Dictionary<Vector3Int, List<Vector3>> shoreCellEdges = new Dictionary<Vector3Int, List<Vector3>>();

            foreach (var cellPos in cellBounds.allPositionsWithin)
            {
                if (!IsWaterTile(cellPos)) continue;

                Vector3 cellCenter = waterTilemap.GetCellCenterWorld(cellPos);
                List<Vector3> nonWaterNeighborCenters = new List<Vector3>();

                foreach (var offset in neighborOffsets)
                {
                    if (!IsWaterTile(cellPos + offset))
                    {
                        Vector3 neighborCenter = waterTilemap.GetCellCenterWorld(cellPos + offset);
                        nonWaterNeighborCenters.Add(neighborCenter);
                    }
                }

                if (nonWaterNeighborCenters.Count > 0)
                {
                    shoreCellEdges[cellPos] = nonWaterNeighborCenters;
                }
            }

            int shoreCellCount = shoreCellEdges.Count;

            // Calculate typical cell distance for shore width scaling
            float typicalCellDist = waterTilemap.cellSize.magnitude * 0.7f;
            float shoreDistanceThreshold = typicalCellDist * shoreWidth * 2f;

            // Now iterate through all pixels and calculate shore intensity
            for (int py = 0; py < texHeight; py++)
            {
                for (int px = 0; px < texWidth; px++)
                {
                    // Convert pixel to world position
                    float u = (float)px / (texWidth - 1);
                    float v = (float)py / (texHeight - 1);

                    Vector3 worldPos = new Vector3(
                        Mathf.Lerp(tilemapBounds.min.x, tilemapBounds.max.x, u),
                        Mathf.Lerp(tilemapBounds.min.y, tilemapBounds.max.y, v),
                        0f
                    );

                    // Check which cell this pixel is in
                    Vector3Int pixelCell = waterTilemap.WorldToCell(worldPos);

                    // Skip if not a water tile
                    if (!IsWaterTile(pixelCell)) continue;

                    // Skip if not a shore cell
                    if (!shoreCellEdges.ContainsKey(pixelCell)) continue;

                    // Find distance to nearest non-water neighbor center
                    float minDistToShore = float.MaxValue;
                    List<Vector3> neighborCenters = shoreCellEdges[pixelCell];

                    foreach (var neighborCenter in neighborCenters)
                    {
                        float dist = Vector3.Distance(worldPos, neighborCenter);
                        minDistToShore = Mathf.Min(minDistToShore, dist);
                    }

                    // Calculate intensity based on distance
                    // Closer to non-water = higher intensity
                    if (minDistToShore < typicalCellDist + shoreDistanceThreshold)
                    {
                        // Add noise to break up the regularity
                        float noiseValue = 0f;
                        if (shoreNoise > 0.01f)
                        {
                            // Sample noise at this world position
                            float nx = worldPos.x * noiseScale;
                            float ny = worldPos.y * noiseScale;
                            noiseValue = FBM(nx, ny, 3); // Returns roughly -1 to 1

                            // Use noise to perturb the distance threshold
                            float distortAmount = noiseValue * shoreNoise * shoreDistanceThreshold * 0.8f;
                            minDistToShore += distortAmount;
                        }

                        // Normalize: 0 = at the edge, 1 = at threshold distance
                        float normalizedDist = (minDistToShore - typicalCellDist * 0.3f) / shoreDistanceThreshold;
                        normalizedDist = Mathf.Clamp01(normalizedDist);

                        // Invert: 1 = close to edge, 0 = far from edge
                        float intensity = 1.0f - normalizedDist;

                        // Apply softness curve
                        intensity = Mathf.Pow(intensity, 1.0f / (1.0f + shoreSoftness));

                        // Add subtle intensity variation from noise
                        if (shoreNoise > 0.01f)
                        {
                            float intensityNoise = FBM(worldPos.x * noiseScale * 2f + 100f, worldPos.y * noiseScale * 2f + 100f, 2);
                            intensity *= 1f + intensityNoise * shoreNoise * 0.3f;
                            intensity = Mathf.Clamp01(intensity);
                        }

                        if (intensity > 0.01f)
                        {
                            int idx = py * texWidth + px;
                            pixels[idx].r = Mathf.Max(pixels[idx].r, intensity);
                            pixels[idx].g = pixels[idx].r;
                            pixels[idx].b = pixels[idx].r;
                            pixels[idx].a = 1f;
                        }
                    }
                }
            }

            if (logDebugInfo)
            {
                Debug.Log($"[WaterShoreDetector] Found {shoreCellCount} shore cells");
            }

            shoreMaskCPU.SetPixels(pixels);
            shoreMaskCPU.Apply();

            // Store preview for inspector
            debugMaskPreview = shoreMaskCPU;

            // Count non-zero pixels for debug
            int nonZeroPixels = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.r > 0.01f) nonZeroPixels++;
            }

            if (logDebugInfo)
            {
                Debug.Log($"[WaterShoreDetector] Generated mask: {texWidth}x{texHeight}, " +
                          $"non-zero pixels: {nonZeroPixels}/{pixels.Length} ({(100f * nonZeroPixels / pixels.Length):F1}%)");
                Debug.Log($"[WaterShoreDetector] Bounds: min={tilemapBounds.min}, max={tilemapBounds.max}, size={tilemapBounds.size}");
            }

            // Apply to material
            ApplyToMaterial();
        }

        private bool IsWaterTile(Vector3Int cellPos)
        {
            TileBase tile = waterTilemap.GetTile(cellPos);
            return tile != null;
        }

        // Simple 2D noise function for organic shore edges
        private float Noise2D(float x, float y)
        {
            // Simple hash-based noise
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;

            // Smooth interpolation
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            // Hash corners
            float a = Hash(ix, iy);
            float b = Hash(ix + 1, iy);
            float c = Hash(ix, iy + 1);
            float d = Hash(ix + 1, iy + 1);

            // Bilinear interpolation
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private float Hash(int x, int y)
        {
            int n = x + y * 57;
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }

        // Fractal Brownian Motion for more natural noise
        private float FBM(float x, float y, int octaves = 3)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * Noise2D(x * frequency, y * frequency);
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return value;
        }

        private void ApplyToMaterial()
        {
            if (waterMaterial == null)
            {
                Debug.LogWarning("WaterShoreDetector: No water material assigned!");
                return;
            }

            waterMaterial.SetTexture(ShoreMaskTexID, shoreMaskCPU);
            waterMaterial.SetVector(ShoreMaskBoundsMinID, new Vector4(tilemapBounds.min.x, tilemapBounds.min.y, tilemapBounds.min.z, 0));
            waterMaterial.SetVector(ShoreMaskBoundsSizeID, new Vector4(tilemapBounds.size.x, tilemapBounds.size.y, tilemapBounds.size.z, 0));
            waterMaterial.SetFloat(UseShoreDetectionID, 1f);

            if (logDebugInfo)
            {
                Debug.Log($"[WaterShoreDetector] Applied to material '{waterMaterial.name}':");
                Debug.Log($"  - _ShoreMaskTex: {(shoreMaskCPU != null ? $"{shoreMaskCPU.width}x{shoreMaskCPU.height}" : "null")}");
                Debug.Log($"  - _ShoreMaskBoundsMin: ({tilemapBounds.min.x:F2}, {tilemapBounds.min.y:F2})");
                Debug.Log($"  - _ShoreMaskBoundsSize: ({tilemapBounds.size.x:F2}, {tilemapBounds.size.y:F2})");
                Debug.Log($"  - _UseShoreDetection: 1");

                // Verify material has the properties
                if (!waterMaterial.HasProperty(ShoreMaskTexID))
                    Debug.LogError("  - Material is missing _ShoreMaskTex property!");
                if (!waterMaterial.HasProperty(UseShoreDetectionID))
                    Debug.LogError("  - Material is missing _UseShoreDetection property!");
            }
        }

        private void CleanupTextures()
        {
            if (shoreMaskTexture != null)
            {
                shoreMaskTexture.Release();
                DestroyImmediate(shoreMaskTexture);
                shoreMaskTexture = null;
            }

            if (shoreMaskCPU != null)
            {
                DestroyImmediate(shoreMaskCPU);
                shoreMaskCPU = null;
            }
        }

        private void OnDestroy()
        {
            CleanupTextures();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || waterTilemap == null) return;

            // Draw tilemap bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(tilemapBounds.center, tilemapBounds.size);

            // Draw shore tiles
            waterTilemap.CompressBounds();
            BoundsInt cellBounds = waterTilemap.cellBounds;

            Vector3Int[] neighborOffsets = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)
            };

            foreach (var pos in cellBounds.allPositionsWithin)
            {
                if (!IsWaterTile(pos)) continue;

                bool isShore = false;
                foreach (var offset in neighborOffsets)
                {
                    if (!IsWaterTile(pos + offset))
                    {
                        isShore = true;
                        break;
                    }
                }

                if (isShore)
                {
                    Vector3 worldPos = waterTilemap.GetCellCenterWorld(pos);
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                    Gizmos.DrawCube(worldPos, waterTilemap.cellSize * 0.8f);
                }
            }
        }
    }
}
