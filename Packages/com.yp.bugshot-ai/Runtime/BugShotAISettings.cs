using System;
using System.IO;
using UnityEngine;

namespace YP.BugShotAI
{
    [Serializable]
    public sealed class BugShotAISettings
    {
        public bool isEnabled = true;
        public bool automaticCaptureEnabled = true;
        public bool captureScreenshots = true;
        public bool captureOnError = true;
        public bool captureOnException = true;
        public bool captureOnAssert = false;
        public bool captureOnWarning = false;
        public float duplicateCooldownSeconds = 2f;
        public int maxRecentEvents = 50;
        public int maxRecentLogs = 80;
        public int maxReportCount = 50;
        public int maxStorageMegabytes = 256;
        public int maxLogChars = 8000;
        public int maxStackTraceChars = 12000;
        public bool maskEmailAddresses = true;
        public bool maskIpAddresses = false;
        public string outputFolderName = BugShotAIConstants.DefaultReportsFolderName;
        public string customOutputDirectory;

        public static BugShotAISettings CreateDefault()
        {
            BugShotAISettings settings = new BugShotAISettings();
            settings.Validate();
            return settings;
        }

        public BugShotAISettings Clone()
        {
            string json = JsonUtility.ToJson(this);
            BugShotAISettings clone = JsonUtility.FromJson<BugShotAISettings>(json);
            clone.Validate();
            return clone;
        }

        public void Validate()
        {
            duplicateCooldownSeconds = Mathf.Max(0f, duplicateCooldownSeconds);
            maxRecentEvents = Mathf.Max(1, maxRecentEvents);
            maxRecentLogs = Mathf.Max(0, maxRecentLogs);
            maxReportCount = Mathf.Max(1, maxReportCount);
            maxStorageMegabytes = Mathf.Max(1, maxStorageMegabytes);
            maxLogChars = Mathf.Max(256, maxLogChars);
            maxStackTraceChars = Mathf.Max(256, maxStackTraceChars);

            if (string.IsNullOrWhiteSpace(outputFolderName))
            {
                outputFolderName = BugShotAIConstants.DefaultReportsFolderName;
            }

            outputFolderName = BugShotAITextUtility.SanitizeFileName(outputFolderName);
        }

        public bool ShouldCapture(LogType type)
        {
            if (!isEnabled || !automaticCaptureEnabled)
            {
                return false;
            }

            return (type == LogType.Error && captureOnError)
                   || (type == LogType.Exception && captureOnException)
                   || (type == LogType.Assert && captureOnAssert)
                   || (type == LogType.Warning && captureOnWarning);
        }
    }

    public static class BugShotAISettingsFile
    {
        public static BugShotAISettings LoadOrDefault(BugShotAISettings fallback)
        {
            BugShotAISettings settings = fallback != null ? fallback.Clone() : BugShotAISettings.CreateDefault();
            string path = GetProjectSettingsPath();

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    BugShotAISettings loaded = JsonUtility.FromJson<BugShotAISettings>(json);
                    if (loaded != null)
                    {
                        settings = loaded;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{BugShotAIConstants.LogPrefix} Failed to read project settings. Using recorder defaults. {ex.Message}");
                }
            }

            settings.Validate();
            return settings;
        }

        public static void Save(BugShotAISettings settings)
        {
            if (settings == null)
            {
                settings = BugShotAISettings.CreateDefault();
            }

            settings.Validate();
            string path = GetProjectSettingsPath();
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Project settings path is unavailable.");
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(settings, true));
        }

        public static string GetProjectSettingsPath()
        {
            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            return string.IsNullOrEmpty(projectRoot)
                ? null
                : Path.Combine(projectRoot, "ProjectSettings", BugShotAIConstants.ProjectSettingsFileName);
        }
    }
}
