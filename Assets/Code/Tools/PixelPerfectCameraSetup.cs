using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Stillwater.Tools
{
    public static class PixelPerfectCameraSetup
    {
        private const int AssetsPixelsPerUnit = 32;
        private const int ReferenceResolutionX = 320;
        private const int ReferenceResolutionY = 180;

        [MenuItem("Stillwater/Camera/Setup Pixel Perfect Camera")]
        public static void SetupPixelPerfectCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No Main Camera found in the scene. Please tag your camera as MainCamera.");
                return;
            }

            var pixelPerfectCamera = mainCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null)
            {
                pixelPerfectCamera = mainCamera.gameObject.AddComponent<PixelPerfectCamera>();
                Debug.Log("Added PixelPerfectCamera component to Main Camera.");
            }

            pixelPerfectCamera.assetsPPU = AssetsPixelsPerUnit;
            pixelPerfectCamera.refResolutionX = ReferenceResolutionX;
            pixelPerfectCamera.refResolutionY = ReferenceResolutionY;
            pixelPerfectCamera.upscaleRT = true;
            pixelPerfectCamera.pixelSnapping = true;

            mainCamera.orthographic = true;

            EditorUtility.SetDirty(mainCamera.gameObject);

            Debug.Log($"Pixel Perfect Camera configured:\n" +
                      $"  - Assets PPU: {AssetsPixelsPerUnit}\n" +
                      $"  - Reference Resolution: {ReferenceResolutionX}x{ReferenceResolutionY}\n" +
                      $"  - Upscale Render Texture: Enabled\n" +
                      $"  - Pixel Snapping: Enabled");
        }

        [MenuItem("Stillwater/Camera/Validate Pixel Perfect Settings")]
        public static void ValidatePixelPerfectSettings()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No Main Camera found in the scene.");
                return;
            }

            var pixelPerfectCamera = mainCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null)
            {
                Debug.LogWarning("PixelPerfectCamera component not found on Main Camera. Run 'Stillwater > Camera > Setup Pixel Perfect Camera' first.");
                return;
            }

            bool isValid = true;
            var issues = new System.Text.StringBuilder();

            if (!mainCamera.orthographic)
            {
                issues.AppendLine("  - Camera is not set to Orthographic projection");
                isValid = false;
            }

            if (pixelPerfectCamera.assetsPPU != AssetsPixelsPerUnit)
            {
                issues.AppendLine($"  - Assets PPU is {pixelPerfectCamera.assetsPPU}, expected {AssetsPixelsPerUnit}");
                isValid = false;
            }

            if (pixelPerfectCamera.refResolutionX != ReferenceResolutionX ||
                pixelPerfectCamera.refResolutionY != ReferenceResolutionY)
            {
                issues.AppendLine($"  - Reference Resolution is {pixelPerfectCamera.refResolutionX}x{pixelPerfectCamera.refResolutionY}, expected {ReferenceResolutionX}x{ReferenceResolutionY}");
                isValid = false;
            }

            if (!pixelPerfectCamera.upscaleRT)
            {
                issues.AppendLine("  - Upscale Render Texture is disabled");
                isValid = false;
            }

            if (isValid)
            {
                Debug.Log("Pixel Perfect Camera settings are valid.");
            }
            else
            {
                Debug.LogWarning($"Pixel Perfect Camera has configuration issues:\n{issues}");
            }
        }
    }
}
