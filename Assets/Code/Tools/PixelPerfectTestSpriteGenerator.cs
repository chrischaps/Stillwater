using UnityEditor;
using UnityEngine;
using System.IO;

namespace Stillwater.Tools
{
    public static class PixelPerfectTestSpriteGenerator
    {
        private const int SpriteSize = 16;
        private const int PixelsPerUnit = 16;

        [MenuItem("Stillwater/Camera/Generate Test Sprite")]
        public static void GenerateTestSprite()
        {
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var clearColor = new Color(0, 0, 0, 0);
            var outlineColor = new Color(0.2f, 0.3f, 0.5f, 1f);
            var fillColor = new Color(0.4f, 0.6f, 0.8f, 1f);
            var highlightColor = new Color(0.8f, 0.9f, 1f, 1f);

            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    texture.SetPixel(x, y, clearColor);
                }
            }

            for (int x = 2; x < SpriteSize - 2; x++)
            {
                texture.SetPixel(x, 2, outlineColor);
                texture.SetPixel(x, SpriteSize - 3, outlineColor);
            }
            for (int y = 2; y < SpriteSize - 2; y++)
            {
                texture.SetPixel(2, y, outlineColor);
                texture.SetPixel(SpriteSize - 3, y, outlineColor);
            }

            for (int y = 3; y < SpriteSize - 3; y++)
            {
                for (int x = 3; x < SpriteSize - 3; x++)
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }

            texture.SetPixel(4, SpriteSize - 5, highlightColor);
            texture.SetPixel(5, SpriteSize - 5, highlightColor);
            texture.SetPixel(4, SpriteSize - 6, highlightColor);

            texture.SetPixel(SpriteSize / 2, SpriteSize / 2, highlightColor);

            texture.Apply();

            string directoryPath = "Assets/Art/Sprites/Test";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string path = $"{directoryPath}/TestSprite_16x16.png";
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
                importer.SaveAndReimport();
            }

            Debug.Log($"Test sprite created at: {path}\n" +
                      $"  - Size: {SpriteSize}x{SpriteSize} pixels\n" +
                      $"  - PPU: {PixelsPerUnit}\n" +
                      $"  - Filter Mode: Point (no filtering)");
        }

        [MenuItem("Stillwater/Camera/Create Test Sprite Object")]
        public static void CreateTestSpriteObject()
        {
            string spritePath = "Assets/Art/Sprites/Test/TestSprite_16x16.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite == null)
            {
                Debug.LogWarning("Test sprite not found. Generating one first...");
                GenerateTestSprite();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            if (sprite == null)
            {
                Debug.LogError("Failed to load or create test sprite.");
                return;
            }

            var go = new GameObject("PixelPerfectTestSprite");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 100;

            go.transform.position = Vector3.zero;

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            Debug.Log("Created test sprite object at origin. Move it around to test pixel-perfect rendering.\n" +
                      "Look for sub-pixel jitter or blurring when the camera moves.");
        }
    }
}
