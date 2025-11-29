using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

namespace Stillwater.Tools
{
    public static class IsometricTileGenerator
    {
        private const int TileWidth = 32;
        private const int TileHeight = 32;
        private const int DiamondHeight = 16; // Cell height is 0.5, so diamond is half the sprite height
        private const int PixelsPerUnit = 32;

        private const string SpritePath = "Assets/Art/Tiles/Placeholder";
        private const string TileAssetPath = "Assets/Art/Tiles/Placeholder";

        [MenuItem("Stillwater/Tiles/Generate All Placeholder Tiles")]
        public static void GenerateAllPlaceholderTiles()
        {
            EnsureDirectoryExists(SpritePath);

            GenerateGroundTile();
            GenerateWaterTile();
            GeneratePropTile();
            GenerateInteractableTile();

            AssetDatabase.Refresh();

            CreateTileAssets();

            Debug.Log("All placeholder tiles generated successfully!");
        }

        [MenuItem("Stillwater/Tiles/Generate Ground Tile")]
        public static void GenerateGroundTile()
        {
            var texture = CreateIsometricTexture();
            var groundColor = new Color(0.4f, 0.55f, 0.3f, 1f);
            var groundDark = new Color(0.3f, 0.4f, 0.2f, 1f);
            var groundLight = new Color(0.5f, 0.65f, 0.35f, 1f);

            DrawIsometricDiamond(texture, groundColor, groundDark, groundLight);
            AddGroundDetails(texture, groundDark, groundLight);

            SaveTileTexture(texture, "Ground_Placeholder");
        }

        [MenuItem("Stillwater/Tiles/Generate Water Tile")]
        public static void GenerateWaterTile()
        {
            var texture = CreateIsometricTexture();
            var waterColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            var waterDark = new Color(0.15f, 0.3f, 0.6f, 1f);
            var waterLight = new Color(0.4f, 0.6f, 0.85f, 1f);

            DrawIsometricDiamond(texture, waterColor, waterDark, waterLight);
            AddWavePattern(texture, waterLight);

            SaveTileTexture(texture, "Water_Placeholder");
        }

        [MenuItem("Stillwater/Tiles/Generate Prop Tile")]
        public static void GeneratePropTile()
        {
            var texture = CreateIsometricTexture();
            var rockColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            var rockDark = new Color(0.35f, 0.35f, 0.35f, 1f);
            var rockLight = new Color(0.65f, 0.65f, 0.65f, 1f);

            DrawRock(texture, rockColor, rockDark, rockLight);

            SaveTileTexture(texture, "Prop_Placeholder");
        }

        [MenuItem("Stillwater/Tiles/Generate Interactable Tile")]
        public static void GenerateInteractableTile()
        {
            var texture = CreateIsometricTexture();
            var markerColor = new Color(1f, 0.8f, 0.2f, 1f);
            var markerDark = new Color(0.8f, 0.6f, 0.1f, 1f);
            var markerLight = new Color(1f, 0.95f, 0.6f, 1f);

            DrawFishingSpotMarker(texture, markerColor, markerDark, markerLight);

            SaveTileTexture(texture, "Interactable_Placeholder");
        }

        private static Texture2D CreateIsometricTexture()
        {
            var texture = new Texture2D(TileWidth, TileHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var clearColor = new Color(0, 0, 0, 0);
            for (int y = 0; y < TileHeight; y++)
            {
                for (int x = 0; x < TileWidth; x++)
                {
                    texture.SetPixel(x, y, clearColor);
                }
            }

            return texture;
        }

        private static void DrawIsometricDiamond(Texture2D texture, Color fill, Color dark, Color light)
        {
            int centerX = TileWidth / 2;
            int baseY = 0;
            int halfWidth = TileWidth / 2;
            int halfHeight = DiamondHeight / 2; // 8 pixels for each half

            // Draw bottom half of diamond (expanding from point to widest)
            for (int y = 0; y < halfHeight; y++)
            {
                int rowWidth = ((y + 1) * halfWidth) / halfHeight;
                int startX = centerX - rowWidth;
                int endX = centerX + rowWidth;

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < TileWidth)
                    {
                        Color pixelColor = fill;
                        if (x == startX) pixelColor = dark;
                        else if (x == endX) pixelColor = light;
                        texture.SetPixel(x, baseY + y, pixelColor);
                    }
                }
            }

            // Draw top half of diamond (contracting from widest to point)
            for (int y = 0; y < halfHeight; y++)
            {
                int rowWidth = ((halfHeight - y) * halfWidth) / halfHeight;
                int startX = centerX - rowWidth;
                int endX = centerX + rowWidth;

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < TileWidth)
                    {
                        Color pixelColor = fill;
                        if (x == startX) pixelColor = dark;
                        else if (x == endX) pixelColor = light;
                        texture.SetPixel(x, baseY + halfHeight + y, pixelColor);
                    }
                }
            }
        }

        private static void AddGroundDetails(Texture2D texture, Color dark, Color light)
        {
            // Adjusted for 16-pixel tall diamond
            texture.SetPixel(10, 4, dark);
            texture.SetPixel(20, 6, dark);
            texture.SetPixel(14, 10, light);
            texture.SetPixel(18, 8, light);
            texture.SetPixel(12, 3, dark);
        }

        private static void AddWavePattern(Texture2D texture, Color waveColor)
        {
            // Adjusted for 16-pixel tall diamond
            for (int i = 0; i < 3; i++)
            {
                int baseX = 10 + i * 4;
                int baseY = 4 + i * 2;
                if (IsInsideDiamond(baseX, baseY))
                {
                    texture.SetPixel(baseX, baseY, waveColor);
                    if (IsInsideDiamond(baseX + 1, baseY))
                        texture.SetPixel(baseX + 1, baseY, waveColor);
                }
            }
        }

        private static void DrawRock(Texture2D texture, Color fill, Color dark, Color light)
        {
            // Rock sitting on isometric ground - fits within 16-pixel height
            int centerX = TileWidth / 2;
            int baseY = 1;
            int rockWidth = 10;
            int rockHeight = 8;

            for (int y = 0; y < rockHeight; y++)
            {
                float t = y / (float)rockHeight;
                int rowWidth = (int)(rockWidth * Mathf.Sin(t * Mathf.PI));
                rowWidth = Mathf.Max(rowWidth, 2);

                int startX = centerX - rowWidth / 2;
                int endX = centerX + rowWidth / 2;

                for (int x = startX; x <= endX; x++)
                {
                    Color pixelColor = fill;
                    if (x == startX) pixelColor = dark;
                    else if (x == endX) pixelColor = light;
                    else if (y > rockHeight * 0.6f) pixelColor = dark;
                    texture.SetPixel(x, baseY + y, pixelColor);
                }
            }

            texture.SetPixel(centerX - 2, baseY + rockHeight - 2, light);
            texture.SetPixel(centerX - 1, baseY + rockHeight - 3, light);
        }

        private static void DrawFishingSpotMarker(Texture2D texture, Color fill, Color dark, Color light)
        {
            // Fishing spot indicator - fits within 16-pixel height
            int centerX = TileWidth / 2;
            int centerY = DiamondHeight / 2; // Center at y=8
            int radius = 4;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float dist = Mathf.Sqrt(x * x + y * y);
                    if (dist <= radius)
                    {
                        int px = centerX + x;
                        int py = centerY + y;

                        if (dist > radius - 1.5f)
                        {
                            Color edgeColor = (x + y < 0) ? light : dark;
                            texture.SetPixel(px, py, edgeColor);
                        }
                        else if (dist <= 1.5f)
                        {
                            texture.SetPixel(px, py, light);
                        }
                        else
                        {
                            texture.SetPixel(px, py, fill);
                        }
                    }
                }
            }
        }

        private static bool IsInsideDiamond(int x, int y)
        {
            int centerX = TileWidth / 2;
            int centerY = DiamondHeight / 2; // Diamond is in bottom 16 pixels
            int halfWidth = TileWidth / 2;
            int halfHeight = DiamondHeight / 2;

            float dx = Mathf.Abs(x - centerX) / (float)halfWidth;
            float dy = Mathf.Abs(y - centerY) / (float)halfHeight;

            return (dx + dy) <= 1f;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void SaveTileTexture(Texture2D texture, string name)
        {
            EnsureDirectoryExists(SpritePath);

            string path = $"{SpritePath}/{name}.png";
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);

            Object.DestroyImmediate(texture);

            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteImportMode = SpriteImportMode.Single;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spritePivot = new Vector2(0.5f, 0f);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
            }

            Debug.Log($"Created tile sprite: {path}");
        }

        private static void CreateTileAssets()
        {
            CreateTileAsset("Ground_Placeholder");
            CreateTileAsset("Water_Placeholder");
            CreateTileAsset("Prop_Placeholder");
            CreateTileAsset("Interactable_Placeholder");
        }

        private static void CreateTileAsset(string name)
        {
            string spritePath = $"{SpritePath}/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite == null)
            {
                Debug.LogWarning($"Could not load sprite at {spritePath}");
                return;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;

            string tilePath = $"{TileAssetPath}/{name}.asset";
            AssetDatabase.CreateAsset(tile, tilePath);

            Debug.Log($"Created tile asset: {tilePath}");
        }
    }
}
