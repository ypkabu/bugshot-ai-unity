using System;
using UnityEngine;

namespace YP.BugShotAI
{
    public static class BugShotAIScreenshotCaptureService
    {
        public static BugShotAIScreenshotResult CapturePng(BugShotAISettings settings)
        {
            if (settings == null || !settings.captureScreenshots)
            {
                return BugShotAIScreenshotResult.NotCaptured(null);
            }

            try
            {
                Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null)
                {
                    // Batchmode may have no render target; the report remains useful without a PNG.
                    return BugShotAIScreenshotResult.NotCaptured("ScreenCapture returned null.");
                }

                byte[] bytes = texture.EncodeToPNG();
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(texture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                return bytes == null || bytes.Length == 0
                    ? BugShotAIScreenshotResult.NotCaptured("PNG encoding returned no data.")
                    : BugShotAIScreenshotResult.Captured(bytes);
            }
            catch (Exception ex)
            {
                return BugShotAIScreenshotResult.NotCaptured(ex.Message);
            }
        }
    }

    public sealed class BugShotAIScreenshotResult
    {
        private BugShotAIScreenshotResult(byte[] pngBytes, string error)
        {
            PngBytes = pngBytes;
            Error = error;
        }

        public byte[] PngBytes { get; }
        public string Error { get; }
        public bool HasScreenshot => PngBytes != null && PngBytes.Length > 0;

        public static BugShotAIScreenshotResult Captured(byte[] pngBytes)
        {
            return new BugShotAIScreenshotResult(pngBytes, null);
        }

        public static BugShotAIScreenshotResult NotCaptured(string error)
        {
            return new BugShotAIScreenshotResult(null, error);
        }
    }
}
