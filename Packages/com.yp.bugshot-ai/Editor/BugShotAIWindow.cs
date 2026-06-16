using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI.Editor
{
    public sealed class BugShotAIWindow : EditorWindow
    {
        private const string PackageName = "com.yp.bugshot-ai";
        private const string RecorderObjectName = "BugShotAI";
        private const string ReportsFolderName = "BugShotAI";
        private const string NoReportPathText = "(none)";

        private Vector2 scrollPosition;

        [MenuItem("Tools/BugShot AI/Open Window")]
        public static void OpenWindow()
        {
            BugShotAIWindow window = GetWindow<BugShotAIWindow>("BugShot AI");
            window.minSize = new Vector2(460f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            string existingLastReportPath = GetExistingLastReportPath();
            bool hasLastReport = !string.IsNullOrEmpty(existingLastReportPath);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Package", PackageName);
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Active Scene", SceneManager.GetActiveScene().name);
            EditorGUILayout.LabelField("Recorder Exists", RecorderExistsInActiveScene() ? "Yes" : "No");
            EditorGUILayout.LabelField("Reports Folder Exists", Directory.Exists(GetReportsFolderPath()) ? "Yes" : "No");
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Reports Folder Path", GUILayout.Width(120f));
                EditorGUILayout.SelectableLabel(GetReportsFolderDisplayPath(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Last Report Path", GUILayout.Width(120f));
                EditorGUILayout.SelectableLabel(GetLastReportDisplayPath(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.Space(10f);

            if (GUILayout.Button("Open Reports Folder", GUILayout.Height(28f)))
            {
                OpenReportsFolder();
            }

            using (new EditorGUI.DisabledScope(!hasLastReport))
            {
                if (GUILayout.Button("Open Last JSON", GUILayout.Height(28f)))
                {
                    OpenLastJson();
                }

                if (GUILayout.Button("Copy Last JSON Path", GUILayout.Height(28f)))
                {
                    CopyLastJsonPath();
                }

                if (GUILayout.Button("Copy GitHub Issue Prompt JP", GUILayout.Height(28f)))
                {
                    CopyGitHubIssuePromptJP();
                }

                if (GUILayout.Button("Copy GitHub Issue Prompt EN", GUILayout.Height(28f)))
                {
                    CopyGitHubIssuePromptEN();
                }

                if (GUILayout.Button("Save GitHub Issue Prompt JP To File", GUILayout.Height(28f)))
                {
                    SaveGitHubIssuePromptJPToFile();
                }

                if (GUILayout.Button("Save GitHub Issue Prompt EN To File", GUILayout.Height(28f)))
                {
                    SaveGitHubIssuePromptENToFile();
                }
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Create BugShotAI Recorder In Scene", GUILayout.Height(28f)))
                {
                    CreateRecorderInScene();
                }
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Trigger Test Error", GUILayout.Height(28f)))
                {
                    TriggerTestError();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static string GetReportsFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, ReportsFolderName);
        }

        private static string GetReportsFolderDisplayPath()
        {
            return BuildSafeFolderDisplayPath(GetReportsFolderPath());
        }

        private static string GetLastReportDisplayPath()
        {
            BugShotAIRecorder recorder = FindRecorderInActiveScene();
            if (recorder != null && !string.IsNullOrEmpty(recorder.LastReportPath))
            {
                return Path.GetFileName(recorder.LastReportPath);
            }

            string latestReportPath = FindLatestReportPath();
            return string.IsNullOrEmpty(latestReportPath) ? NoReportPathText : Path.GetFileName(latestReportPath);
        }

        private static void OpenReportsFolder()
        {
            string reportsFolderPath = GetReportsFolderPath();
            Directory.CreateDirectory(reportsFolderPath);
            EditorUtility.RevealInFinder(reportsFolderPath);
        }

        private static void OpenLastJson()
        {
            string reportPath = GetExistingLastReportPath();
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning("[BugShotAI] No JSON report exists to open.");
                return;
            }

            EditorUtility.OpenWithDefaultApp(reportPath);
        }

        private static void CopyLastJsonPath()
        {
            string reportPath = GetExistingLastReportPath();
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning("[BugShotAI] No JSON report path exists to copy.");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = reportPath;
            Debug.Log($"[BugShotAI] Copied JSON report path: {reportPath}");
        }

        private static void CopyGitHubIssuePromptJP()
        {
            string reportPath = GetExistingLastReportPath();
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning("[BugShotAI] No JSON report exists for a Japanese GitHub Issue prompt.");
                return;
            }

            string json = File.ReadAllText(reportPath);
            string prompt = BuildGitHubIssuePromptJP(reportPath, json);
            EditorGUIUtility.systemCopyBuffer = prompt;
            Debug.Log($"[BugShotAI] Copied Japanese GitHub Issue prompt for report: {reportPath}");
        }

        private static void CopyGitHubIssuePromptEN()
        {
            string reportPath = GetExistingLastReportPath();
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning("[BugShotAI] No JSON report exists for an English GitHub Issue prompt.");
                return;
            }

            string json = File.ReadAllText(reportPath);
            string prompt = BuildGitHubIssuePromptEN(reportPath, json);
            EditorGUIUtility.systemCopyBuffer = prompt;
            Debug.Log($"[BugShotAI] Copied English GitHub Issue prompt for report: {reportPath}");
        }

        private static void SaveGitHubIssuePromptJPToFile()
        {
            SaveGitHubIssuePromptToFile("jp", "Japanese", BuildGitHubIssuePromptJP);
        }

        private static void SaveGitHubIssuePromptENToFile()
        {
            SaveGitHubIssuePromptToFile("en", "English", BuildGitHubIssuePromptEN);
        }

        private static void SaveGitHubIssuePromptToFile(string languageSuffix, string languageLabel, Func<string, string, string> buildPrompt)
        {
            string reportPath = GetExistingLastReportPath();
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning($"[BugShotAI] No JSON report exists for a {languageLabel} GitHub Issue prompt file.");
                return;
            }

            string reportsFolderPath = GetReportsFolderPath();
            Directory.CreateDirectory(reportsFolderPath);

            string json = File.ReadAllText(reportPath);
            string prompt = buildPrompt(reportPath, json);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string promptPath = Path.Combine(reportsFolderPath, $"bugshot_issue_prompt_{timestamp}_{languageSuffix}.md");
            File.WriteAllText(promptPath, prompt, Encoding.UTF8);
            Debug.Log($"[BugShotAI] Saved {languageLabel} GitHub Issue prompt: {NormalizePath(promptPath)}");
        }

        private static void CreateRecorderInScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[BugShotAI] Cannot create recorder because there is no valid active scene.");
                return;
            }

            BugShotAIRecorder recorder = FindRecorderInScene(activeScene);
            if (recorder != null)
            {
                Selection.activeGameObject = recorder.gameObject;
                Debug.Log("[BugShotAI] Found existing BugShotAIRecorder in the active scene. No new GameObject was created.");
                return;
            }

            GameObject recorderObject = new GameObject(RecorderObjectName);
            Undo.RegisterCreatedObjectUndo(recorderObject, "Create BugShotAI Recorder");
            Undo.AddComponent<BugShotAIRecorder>(recorderObject);
            Selection.activeGameObject = recorderObject;
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[BugShotAI] Created BugShotAI GameObject and added BugShotAIRecorder in the active scene.");
        }

        private static void TriggerTestError()
        {
            BugShotAIEventLogger.Record("EditorWindow", "Triggered test error from BugShot AI Window");
            Debug.LogError("[BugShotAI] Test error triggered from editor window.");

            BugShotAIRecorder recorder = FindRecorderInActiveScene();
            string reportPath = recorder != null ? recorder.LastReportPath : null;
            if (string.IsNullOrEmpty(reportPath))
            {
                Debug.LogWarning("[BugShotAI] Triggered test error, but no JSON report path is available yet.");
                return;
            }

            Debug.Log($"[BugShotAI] Last report saved: {reportPath}");
        }

        private static string GetExistingLastReportPath()
        {
            BugShotAIRecorder recorder = FindRecorderInActiveScene();
            if (recorder != null && !string.IsNullOrEmpty(recorder.LastReportPath) && File.Exists(recorder.LastReportPath))
            {
                return recorder.LastReportPath;
            }

            string latestReportPath = FindLatestReportPath();
            return !string.IsNullOrEmpty(latestReportPath) && File.Exists(latestReportPath)
                ? latestReportPath
                : null;
        }

        private static string BuildGitHubIssuePromptJP(string reportPath, string json)
        {
            string promptJson = SanitizeJsonForPrompt(reportPath, json);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("以下のBugShot AI JSONレポートをもとに、GitHub Issueを生成してください。");
            builder.AppendLine();
            builder.AppendLine("出力ルール:");
            builder.AppendLine("- Markdown形式で出力してください。");
            builder.AppendLine("- Title / Summary / Environment / Steps to Reproduce / Expected Result / Actual Result / Logs / Screenshot / Severity を生成してください。");
            builder.AppendLine("- Severity は Critical / High / Medium / Low から1つ選んでください。");
            builder.AppendLine("- 不明な情報は推測せず Unknown と書いてください。");
            builder.AppendLine("- recentEvents がある場合は再現手順の推定に使ってください。");
            builder.AppendLine("- stackTrace から重要そうな行を抜き出してください。");
            builder.AppendLine();
            builder.AppendLine($"JSON Report File: {Path.GetFileName(reportPath)}");
            builder.AppendLine();
            builder.AppendLine("JSON:");
            builder.AppendLine("```json");
            builder.AppendLine(promptJson);
            builder.AppendLine("```");
            return builder.ToString();
        }

        private static string BuildGitHubIssuePromptEN(string reportPath, string json)
        {
            string promptJson = SanitizeJsonForPrompt(reportPath, json);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Create a GitHub Issue from the following BugShot AI JSON report.");
            builder.AppendLine();
            builder.AppendLine("Output rules:");
            builder.AppendLine("- Output a Markdown GitHub Issue.");
            builder.AppendLine("- Include Title, Summary, Environment, Steps to Reproduce, Expected Result, Actual Result, Logs, Screenshot, and Severity.");
            builder.AppendLine("- Do not invent unknown information.");
            builder.AppendLine("- Use `Unknown` when information is missing.");
            builder.AppendLine("- Use recentEvents to infer reproduction steps when possible.");
            builder.AppendLine("- Extract important lines from stackTrace.");
            builder.AppendLine("- Choose Severity from Critical / High / Medium / Low.");
            builder.AppendLine();
            builder.AppendLine($"JSON Report File: {Path.GetFileName(reportPath)}");
            builder.AppendLine();
            builder.AppendLine("JSON:");
            builder.AppendLine("```json");
            builder.AppendLine(promptJson);
            builder.AppendLine("```");
            return builder.ToString();
        }

        private static bool RecorderExistsInActiveScene()
        {
            return FindRecorderInActiveScene() != null;
        }

        private static BugShotAIRecorder FindRecorderInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() ? FindRecorderInScene(activeScene) : null;
        }

        private static BugShotAIRecorder FindRecorderInScene(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                BugShotAIRecorder recorder = rootObject.GetComponentInChildren<BugShotAIRecorder>(true);
                if (recorder != null)
                {
                    return recorder;
                }
            }

            return null;
        }

        private static string FindLatestReportPath()
        {
            string reportsFolderPath = GetReportsFolderPath();
            if (!Directory.Exists(reportsFolderPath))
            {
                return null;
            }

            return Directory
                .EnumerateFiles(reportsFolderPath, "bugshot_*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private static string BuildSafeFolderDisplayPath(string path)
        {
            string normalizedPath = NormalizePath(path).TrimEnd('/');
            string[] parts = normalizedPath.Split('/');
            if (parts.Length >= 2)
            {
                return $".../{parts[parts.Length - 2]}/{parts[parts.Length - 1]}/";
            }

            return string.IsNullOrEmpty(normalizedPath) ? NoReportPathText : $".../{normalizedPath}/";
        }

        private static string SanitizeJsonForPrompt(string reportPath, string json)
        {
            if (string.IsNullOrEmpty(reportPath) || string.IsNullOrEmpty(json))
            {
                return json;
            }

            string reportFolderPath = Path.GetDirectoryName(reportPath);
            if (string.IsNullOrEmpty(reportFolderPath))
            {
                return json;
            }

            string normalizedReportFolderPath = NormalizePath(reportFolderPath).TrimEnd('/');
            string safeReportFolderPath = GetReportsFolderDisplayPath().TrimEnd('/');
            return json.Replace(normalizedReportFolderPath + "/", safeReportFolderPath + "/");
        }
    }
}
