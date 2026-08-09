using System;
using UnityEditor;
using UnityEngine;

namespace YP.BugShotAI.Editor
{
    internal static class BugShotAIEditorSettingsUtility
    {
        public static BugShotAISettings LoadSettings()
        {
            return BugShotAISettingsFile.LoadOrDefault(BugShotAISettings.CreateDefault());
        }

        public static void SaveSettings(BugShotAISettings settings)
        {
            BugShotAISettingsFile.Save(settings);
            AssetDatabase.Refresh();
        }

        public static bool DrawSettingsFields(BugShotAISettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            EditorGUI.BeginChangeCheck();

            settings.isEnabled = EditorGUILayout.Toggle("Enabled", settings.isEnabled);
            settings.automaticCaptureEnabled = EditorGUILayout.Toggle("Automatic Capture", settings.automaticCaptureEnabled);
            settings.captureScreenshots = EditorGUILayout.Toggle("Capture Screenshots", settings.captureScreenshots);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Capture Targets", EditorStyles.boldLabel);
            settings.captureOnError = EditorGUILayout.Toggle("Error", settings.captureOnError);
            settings.captureOnException = EditorGUILayout.Toggle("Exception", settings.captureOnException);
            settings.captureOnAssert = EditorGUILayout.Toggle("Assert", settings.captureOnAssert);
            settings.captureOnWarning = EditorGUILayout.Toggle("Warning", settings.captureOnWarning);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel);
            settings.duplicateCooldownSeconds = EditorGUILayout.FloatField("Duplicate Cooldown", settings.duplicateCooldownSeconds);
            settings.maxRecentEvents = EditorGUILayout.IntField("Max Recent Events", settings.maxRecentEvents);
            settings.maxRecentLogs = EditorGUILayout.IntField("Max Recent Logs", settings.maxRecentLogs);
            settings.maxReportCount = EditorGUILayout.IntField("Max Report Count", settings.maxReportCount);
            settings.maxStorageMegabytes = EditorGUILayout.IntField("Max Storage MB", settings.maxStorageMegabytes);
            settings.maxLogChars = EditorGUILayout.IntField("Max Log Chars", settings.maxLogChars);
            settings.maxStackTraceChars = EditorGUILayout.IntField("Max StackTrace Chars", settings.maxStackTraceChars);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Privacy", EditorStyles.boldLabel);
            settings.maskEmailAddresses = EditorGUILayout.Toggle("Mask Emails", settings.maskEmailAddresses);
            settings.maskIpAddresses = EditorGUILayout.Toggle("Mask IP Addresses", settings.maskIpAddresses);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            settings.outputFolderName = EditorGUILayout.TextField("Output Folder Name", settings.outputFolderName);

            using (new EditorGUILayout.HorizontalScope())
            {
                settings.customOutputDirectory = EditorGUILayout.TextField("Custom Output Directory", settings.customOutputDirectory);
                if (GUILayout.Button("...", GUILayout.Width(32f)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Select BugShot Reports Folder", settings.customOutputDirectory, string.Empty);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        settings.customOutputDirectory = selected;
                    }
                }
            }

            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                settings.Validate();
            }

            return changed;
        }

        public static string BuildSafePathLabel(string path, bool fileNameOnly)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "(none)";
            }

            string normalized = BugShotAITextUtility.NormalizePath(path);
            if (fileNameOnly)
            {
                return System.IO.Path.GetFileName(normalized);
            }

            string trimmed = normalized.TrimEnd('/');
            string[] parts = trimmed.Split('/');
            if (parts.Length >= 2)
            {
                return $".../{parts[parts.Length - 2]}/{parts[parts.Length - 1]}/";
            }

            return ".../" + trimmed + "/";
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{BugShotAIConstants.LogPrefix} {message}");
        }

        public static void Log(string message)
        {
            Debug.Log($"{BugShotAIConstants.LogPrefix} {message}");
        }
    }
}
