using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YP.BugShotAI.Tests
{
    public static class BugShotAIExampleReportGenerator
    {
        [MenuItem("Tools/BugShot AI/Generate Documentation Example Report")]
        public static void Generate()
        {
            string packageRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", BugShotAIConstants.PackageName));
            string documentationRoot = Path.Combine(packageRoot, "Documentation~");
            string exampleDirectory = Path.Combine(documentationRoot, "ExampleReport");

            if (Directory.Exists(exampleDirectory))
            {
                Directory.Delete(exampleDirectory, true);
            }

            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.maskIpAddresses = true;
            settings.maxReportCount = 100;
            settings.maxStorageMegabytes = 100;

            BugShotAIReport report = CreateUnsanitizedDemoReport();
            BugShotAIPrivacySanitizer.SanitizeInPlace(report, settings);

            BugShotAIReportStorage storage = new BugShotAIReportStorage(documentationRoot);
            BugShotAISaveResult result = storage.Save(report, CreateDemoScreenshotPng(), settings);

            Debug.Log($"{BugShotAIConstants.LogPrefix} Generated documentation example report: {result.reportDirectoryPath}");
            AssetDatabase.Refresh();
        }

        private static BugShotAIReport CreateUnsanitizedDemoReport()
        {
            return new BugShotAIReport
            {
                schemaVersion = BugShotAIConstants.ReportSchemaVersion,
                reportId = "ExampleReport",
                fingerprint = "demo1234abcd5678",
                occurrenceCount = 3,
                firstOccurrenceUtc = "2026-07-31T00:00:01Z",
                timestampUtc = "2026-07-31T00:00:03Z",
                sceneName = "DemoScene",
                scenePath = "Assets/Scenes/DemoScene.unity",
                projectName = "BugShotAI Demo",
                logType = "Error",
                condition = "Player fell through the floor near the right platform.",
                stackTrace =
                    "UnityEngine.Debug:LogError (object)\n" +
                    "BugShotAIDemoBugTrigger:TriggerDemoBug () (at C:/Users/demo-user/SecretProject/Assets/Samples/BugShotAI/BasicSetup/BugShotAIDemoBugTrigger.cs:63)\n" +
                    "DemoService:SendReport () (at /Users/demo-user/SecretProject/Assets/DemoService.cs:24)\n" +
                    "LinuxRunner:Execute () (at /home/demo-user/SecretProject/Assets/LinuxRunner.cs:18)\n" +
                    "NetworkShare:Read () (at \\\\BuildShare\\Users\\demo-user\\SecretProject\\Assets\\NetworkShare.cs:7)\n" +
                    "Authorization: Bearer demo-secret-token api_key=demo-api-key access_token=demo-access-token\n" +
                    "https://example.invalid/callback#access_token=fragment-secret\n" +
                    "Contact: demo-user@example.invalid\n" +
                    "IP: 192.168.10.24\n",
                fps = 60f,
                isPlaying = true,
                editorState = new BugShotAIEditorState
                {
                    isEditor = true,
                    isPlaying = true,
                    isBatchMode = false,
                    platform = "WindowsEditor"
                },
                userNotes = new BugShotAIUserNotes
                {
                    reproductionSteps = "Press D, LeftShift, Space, then B in DemoScene.",
                    expectedResult = "The player should remain on the right platform.",
                    actualResult = "The player Y position dropped below the expected floor height.",
                    notes = "Sanitizer demo input included dummy user paths, email, token, URL fragment token, and IP address."
                },
                playerPosition = new BugShotAIPlayerPosition
                {
                    hasPlayer = true,
                    x = 2.5f,
                    y = -3.2f,
                    z = 0f
                },
                environment = new BugShotAIEnvironment
                {
                    unityVersion = "6000.4.6f1",
                    platform = "WindowsEditor",
                    operatingSystem = "Windows 11",
                    deviceModel = "Demo Workstation",
                    systemMemorySize = 32768,
                    graphicsDeviceName = "Demo GPU",
                    projectName = "BugShotAI Demo",
                    productName = "BugShotAI Demo",
                    companyName = "YP",
                    packageVersion = BugShotAIConstants.PackageVersion
                },
                recentEvents = new[]
                {
                    CreateEvent("Player", "Pressed move right"),
                    CreateEvent("Player", "Pressed dash"),
                    CreateEvent("Player", "Pressed jump"),
                    CreateEvent("Player", "Moved to right platform"),
                    CreateEvent("Player", "Pressed dash before jump"),
                    CreateEvent("Player", "Jumped near platform edge"),
                    CreateEvent("Bug", "Player Y position dropped below expected floor height")
                },
                recentLogs = new[]
                {
                    new BugShotAILogEntry
                    {
                        timestampUtc = "2026-07-31T00:00:02Z",
                        timeSinceStartup = 2.95f,
                        logType = "Log",
                        message = "Loaded config from C:/Users/demo-user/SecretProject/config.json with token=demo-token",
                        stackTrace = null
                    },
                    new BugShotAILogEntry
                    {
                        timestampUtc = "2026-07-31T00:00:03Z",
                        timeSinceStartup = 3.12f,
                        logType = "Error",
                        message = "Player fell through the floor near the right platform.",
                        stackTrace = "BugShotAIDemoBugTrigger:TriggerDemoBug ()"
                    }
                }
            };
        }

        private static BugShotAIEvent CreateEvent(string category, string message)
        {
            return new BugShotAIEvent
            {
                timestampUtc = "2026-07-31T00:00:02Z",
                timeSinceStartup = 2.9f,
                category = category,
                message = message
            };
        }

        private static byte[] CreateDemoScreenshotPng()
        {
            Texture2D texture = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            Color32 background = new Color32(24, 28, 36, 255);
            Color32 platform = new Color32(90, 146, 110, 255);
            Color32 player = new Color32(235, 78, 78, 255);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, background);
                }
            }

            FillRect(texture, 40, 62, 95, 12, platform);
            FillRect(texture, 178, 62, 102, 12, platform);
            FillRect(texture, 224, 28, 18, 18, player);
            byte[] bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }

        private static void FillRect(Texture2D texture, int left, int bottom, int width, int height, Color32 color)
        {
            for (int y = bottom; y < bottom + height; y++)
            {
                for (int x = left; x < left + width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }
}
