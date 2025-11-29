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
            int height = TileHeight / 2;

            for (int y = 0; y < height; y++)
            {
                int rowWidth = (y * halfWidth) / height;
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

            for (int y = 0; y < height; y++)
            {
                int rowWidth = ((height - 1 - y) * halfWidth) / height;
                int startX = centerX - rowWidth;
                int endX = centerX + rowWidth;

                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < TileWidth)
                    {
                        Color pixelColor = fill;
                        if (x == startX) pixelColor = dark;
                        else if (x == endX) pixelColor = light;
                        texture.SetPixel(x, baseY + height + y, pixelColor);
                    }
                }
            }
        }

        private static void AddGroundDetails(Texture2D texture, Color dark, Color light)
        {
            texture.SetPixel(10, 6, dark);
            texture.SetPixel(20, 10, dark);
            texture.SetPixel(14, 12, light);
            texture.SetPixel(18, 8, light);
            texture.SetPixel(12, 4, dark);
        }

        private static void AddWavePattern(Texture2D texture, Color waveColor)
        {
            for (int i = 0; i < 3; i++)
            {
                int baseX = 10 + i * 4;
                int baseY = 6 + i * 2;
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
            int centerX = TileWidth / 2;
            int baseY = 2;
            int rockWidth = 12;
            int rockHeight = 10;

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
            int centerX = TileWidth / 2;
            int centerY = TileHeight / 2 - 2;
            int radius = 5;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float dist = Mathf.Sqrt(x * x + y * y);
                    if (dist <= radius)
                    {
                        int px = centerX + x;
                        int py = centerY + y;

                        if (dist > radius - 2)
                        {
                            Color edgeColor = (x + y < 0) ? light : dark;
                            texture.SetPixel(px, py, edgeColor);
                        }
                        else if (dist <= 2)
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

            texture.SetPixel(centerX, centerY + radius + 2, fill);
            texture.SetPixel(centerX, centerY + radius + 3, fill);
            texture.SetPixel(centerX, centerY - radius - 1, fill);
        }

        private static bool IsInsideDiamond(int x, int y)
        {
            int centerX = TileWidth / 2;
            int centerY = TileHeight / 2;
            int halfWidth = TileWidth / 2;
            int halfHeight = TileHeight / 2;

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
