using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI
{
    public static class BugShotAIReportBuilder
    {
        public static BugShotAIReport Build(BugShotAIReportBuildContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            DateTime utcNow = context.UtcNow == default ? DateTime.UtcNow : context.UtcNow;
            string logType = context.LogType.ToString();
            string fingerprint = BugShotAIFingerprint.Generate(logType, context.Condition, context.StackTrace);
            string reportId = BugShotAIFingerprint.CreateReportId(utcNow, fingerprint);
            Scene scene = SceneManager.GetActiveScene();
            BugShotAIEnvironment environment = CollectEnvironment();

            return new BugShotAIReport
            {
                schemaVersion = BugShotAIConstants.ReportSchemaVersion,
                reportId = reportId,
                fingerprint = fingerprint,
                occurrenceCount = Math.Max(1, context.OccurrenceCount),
                firstOccurrenceUtc = BugShotAITextUtility.NullIfEmpty(context.FirstOccurrenceUtc),
                timestampUtc = utcNow.ToString("o"),
                sceneName = GetSceneName(scene),
                scenePath = BugShotAITextUtility.NormalizePath(scene.path),
                projectName = environment.projectName,
                logType = logType,
                condition = BugShotAITextUtility.NullIfEmpty(BugShotAITextUtility.Truncate(context.Condition, context.Settings.maxLogChars)),
                stackTrace = BugShotAITextUtility.NullIfEmpty(BugShotAITextUtility.Truncate(context.StackTrace, context.Settings.maxStackTraceChars)),
                screenshotError = BugShotAITextUtility.NullIfEmpty(context.ScreenshotError),
                fps = context.Fps,
                isPlaying = Application.isPlaying,
                editorState = CollectEditorState(),
                userNotes = new BugShotAIUserNotes
                {
                    reproductionSteps = BugShotAITextUtility.NullIfEmpty(context.ReproductionSteps),
                    expectedResult = BugShotAITextUtility.NullIfEmpty(context.ExpectedResult),
                    actualResult = BugShotAITextUtility.NullIfEmpty(context.ActualResult),
                    notes = BugShotAITextUtility.NullIfEmpty(context.Notes)
                },
                playerPosition = CreatePlayerPosition(context.PlayerTransform),
                environment = environment,
                recentEvents = context.RecentEvents ?? Array.Empty<BugShotAIEvent>(),
                recentLogs = context.RecentLogs ?? Array.Empty<BugShotAILogEntry>()
            };
        }

        public static BugShotAIEnvironment CollectEnvironment()
        {
            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            string projectName = string.IsNullOrEmpty(projectRoot) ? Application.productName : Path.GetFileName(projectRoot);

            return new BugShotAIEnvironment
            {
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                projectName = BugShotAITextUtility.NullIfEmpty(projectName),
                productName = BugShotAITextUtility.NullIfEmpty(Application.productName),
                companyName = BugShotAITextUtility.NullIfEmpty(Application.companyName),
                packageVersion = BugShotAIConstants.PackageVersion
            };
        }

        public static BugShotAIEditorState CollectEditorState()
        {
            return new BugShotAIEditorState
            {
                isEditor = Application.isEditor,
                isPlaying = Application.isPlaying,
                isBatchMode = Application.isBatchMode,
                platform = Application.platform.ToString()
            };
        }

        private static string GetSceneName(Scene scene)
        {
            return string.IsNullOrEmpty(scene.name) ? "(Untitled Scene)" : scene.name;
        }

        private static BugShotAIPlayerPosition CreatePlayerPosition(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return new BugShotAIPlayerPosition
                {
                    hasPlayer = false
                };
            }

            Vector3 position = playerTransform.position;
            return new BugShotAIPlayerPosition
            {
                hasPlayer = true,
                x = position.x,
                y = position.y,
                z = position.z
            };
        }
    }

    public sealed class BugShotAIReportBuildContext
    {
        public DateTime UtcNow;
        public BugShotAISettings Settings;
        public string Condition;
        public string StackTrace;
        public LogType LogType;
        public float Fps;
        public Transform PlayerTransform;
        public int OccurrenceCount;
        public string FirstOccurrenceUtc;
        public string ScreenshotError;
        public string ReproductionSteps;
        public string ExpectedResult;
        public string ActualResult;
        public string Notes;
        public BugShotAIEvent[] RecentEvents;
        public BugShotAILogEntry[] RecentLogs;
    }
}
