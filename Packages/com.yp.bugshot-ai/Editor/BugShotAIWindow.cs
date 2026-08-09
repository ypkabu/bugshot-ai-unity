using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI.Editor
{
    public sealed class BugShotAIWindow : EditorWindow
    {
        private const string RecorderObjectName = "BugShotAI";

        private readonly List<BugShotAIReportSummary> reports = new List<BugShotAIReportSummary>();
        private BugShotAISettings settings;
        private BugShotAIReport selectedReport;
        private Texture2D screenshotPreview;
        private Vector2 reportsScrollPosition;
        private Vector2 detailsScrollPosition;
        private int selectedReportIndex = -1;
        private bool privacyPreviewFoldout = true;
        private string reproductionSteps;
        private string expectedResult;
        private string actualResult;
        private string notes;
        private EditorApplication.CallbackFunction reportPollCallback;
        private string reportPollPreviousPath;
        private double reportPollDeadline;

        [MenuItem("Tools/BugShot AI/Open Window")]
        public static void OpenWindow()
        {
            BugShotAIWindow window = GetWindow<BugShotAIWindow>("BugShot AI");
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCaptureGuard()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            BugShotAIRecorder[] recorders = Resources.FindObjectsOfTypeAll<BugShotAIRecorder>();
            foreach (BugShotAIRecorder recorder in recorders)
            {
                if (recorder != null && recorder.gameObject.scene.IsValid())
                {
                    recorder.FlushPendingCapturesBeforePlayModeExit();
                }
            }
        }

        private void OnEnable()
        {
            minSize = new Vector2(760f, 520f);
            settings = BugShotAIEditorSettingsUtility.LoadSettings();
            RefreshReports();
        }

        private void OnDisable()
        {
            StopReportPoll();
            DestroyScreenshotPreview();
        }

        private void OnFocus()
        {
            settings = BugShotAIEditorSettingsUtility.LoadSettings();
            RefreshReports();
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                settings = BugShotAIEditorSettingsUtility.LoadSettings();
            }

            DrawToolbar();
            DrawRecordingStatus();
            DrawReports();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"BugShot AI  {BugShotAIConstants.PackageVersion}", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshReports();
                }

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
                {
                    SettingsService.OpenProjectSettings("Project/BugShot AI");
                }
            }
        }

        private void DrawRecordingStatus()
        {
            string rootPath = GetReportsRootPath();
            string lastJsonPath = GetLastReportPath();
            BugShotAIRecorder recorder = FindRecorderInActiveScene();
            bool captureEnabled = settings != null && settings.isEnabled && settings.automaticCaptureEnabled;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Recording", EditorStyles.boldLabel, GUILayout.Width(72f));
                    EditorGUILayout.LabelField(recorder == null ? "Recorder missing" : captureEnabled ? "Ready" : "Disabled in Settings");
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open Reports Folder", GUILayout.Width(138f)))
                    {
                        OpenReportsFolder();
                    }
                }

                EditorGUILayout.LabelField("Active Scene", GetActiveSceneName());
                EditorGUILayout.LabelField("Reports Folder", BugShotAIEditorSettingsUtility.BuildSafePathLabel(rootPath, false));
                EditorGUILayout.LabelField("Latest Report", BugShotAIEditorSettingsUtility.BuildSafePathLabel(lastJsonPath, true));

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                    {
                        if (GUILayout.Button(recorder == null ? "Create Recorder In Scene" : "Select Recorder"))
                        {
                            CreateRecorderInScene();
                        }
                    }

                    using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || recorder == null))
                    {
                        if (GUILayout.Button("Trigger Test Error"))
                        {
                            TriggerTestError();
                        }
                    }
                }
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawReports()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawReportList();
                GUILayout.Space(4f);
                DrawReportDetails();
            }
        }

        private void DrawReportList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(230f), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField($"Reports ({reports.Count})", EditorStyles.boldLabel);
                reportsScrollPosition = EditorGUILayout.BeginScrollView(
                    reportsScrollPosition,
                    GUIStyle.none,
                    GUI.skin.verticalScrollbar,
                    GUILayout.ExpandHeight(true));

                if (reports.Count == 0)
                {
                    EditorGUILayout.HelpBox("No reports have been saved.", MessageType.Info);
                }

                for (int i = 0; i < reports.Count; i++)
                {
                    BugShotAIReportSummary summary = reports[i];
                    GUIStyle style = new GUIStyle(i == selectedReportIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        fixedHeight = 0f,
                        wordWrap = true
                    };
                    string label = BuildReportListLabel(summary);
                    if (GUILayout.Button(label, style, GUILayout.Width(200f), GUILayout.Height(62f)))
                    {
                        SelectReport(i);
                    }
                }

                EditorGUILayout.EndScrollView();

                using (new EditorGUI.DisabledScope(reports.Count == 0))
                {
                    if (GUILayout.Button("Delete All", GUILayout.Height(22f)))
                    {
                        DeleteAllReports();
                    }
                }
            }
        }

        private void DrawReportDetails()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
                detailsScrollPosition = EditorGUILayout.BeginScrollView(detailsScrollPosition, EditorStyles.helpBox, GUILayout.ExpandHeight(true));

                if (selectedReport == null)
                {
                    EditorGUILayout.HelpBox("Select a report to inspect its captured context.", MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    return;
                }

                DrawErrorDetails();
                DrawEnvironmentDetails();
                DrawScreenshotPreview();
                DrawUserNotes();
                DrawPrivacyPreview();
                DrawExportActions();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawErrorDetails()
        {
            float contentWidth = GetDetailsContentWidth();
            GUIStyle readOnlyTextArea = new GUIStyle(EditorStyles.textArea)
            {
                clipping = TextClipping.Clip,
                wordWrap = true
            };

            EditorGUILayout.LabelField("Error", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", BugShotAITextUtility.UnknownIfEmpty(selectedReport.logType));
            EditorGUILayout.LabelField("Scene", BugShotAITextUtility.UnknownIfEmpty(selectedReport.sceneName));
            EditorGUILayout.LabelField("Timestamp UTC", BugShotAITextUtility.UnknownIfEmpty(selectedReport.timestampUtc));
            EditorGUILayout.LabelField("Occurrences", selectedReport.occurrenceCount <= 0 ? "Unknown" : selectedReport.occurrenceCount.ToString());
            EditorGUILayout.LabelField("Fingerprint", BugShotAITextUtility.UnknownIfEmpty(selectedReport.fingerprint));
            EditorGUILayout.LabelField("Message");
            EditorGUILayout.SelectableLabel(
                BugShotAITextUtility.UnknownIfEmpty(selectedReport.condition),
                readOnlyTextArea,
                GUILayout.MaxWidth(contentWidth),
                GUILayout.MinHeight(38f));
            EditorGUILayout.LabelField("Stack Trace");
            EditorGUILayout.SelectableLabel(
                BugShotAITextUtility.UnknownIfEmpty(selectedReport.stackTrace),
                readOnlyTextArea,
                GUILayout.MaxWidth(contentWidth),
                GUILayout.Height(110f));
        }

        private void DrawEnvironmentDetails()
        {
            BugShotAIEnvironment environment = selectedReport.environment;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unity", BugShotAITextUtility.UnknownIfEmpty(environment?.unityVersion));
            EditorGUILayout.LabelField("Platform", BugShotAITextUtility.UnknownIfEmpty(environment?.platform));
            EditorGUILayout.LabelField("Operating System", BugShotAITextUtility.UnknownIfEmpty(environment?.operatingSystem));
            EditorGUILayout.LabelField("Graphics", BugShotAITextUtility.UnknownIfEmpty(environment?.graphicsDeviceName));
            EditorGUILayout.LabelField("Product", BugShotAITextUtility.UnknownIfEmpty(environment?.productName));
        }

        private void DrawExportActions()
        {
            BugShotAIReportSummary summary = GetSelectedOrLatestReport();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("JSON", BugShotAIEditorSettingsUtility.BuildSafePathLabel(summary?.jsonPath, true));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Report"))
                {
                    SaveSelectedReport();
                }

                if (GUILayout.Button("Open Folder"))
                {
                    OpenReportFolder(summary);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open JSON"))
                {
                    OpenJson(summary);
                }

                if (GUILayout.Button("Copy JSON Path"))
                {
                    CopyJsonPath(summary);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Markdown"))
                {
                    CopyMarkdown();
                }

                if (GUILayout.Button("Copy Prompt EN"))
                {
                    CopyPrompt(BugShotAIPromptLanguage.English);
                }

                if (GUILayout.Button("Copy Prompt JP"))
                {
                    CopyPrompt(BugShotAIPromptLanguage.Japanese);
                }
            }

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Delete Report"))
            {
                DeleteSelectedReport();
            }
        }

        private void DrawScreenshotPreview()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Screenshot", EditorStyles.boldLabel);

            if (screenshotPreview == null)
            {
                EditorGUILayout.LabelField("No screenshot.");
                return;
            }

            float width = Mathf.Min(position.width - 240f, 360f);
            float height = width * screenshotPreview.height / Mathf.Max(1f, screenshotPreview.width);
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, screenshotPreview, null, ScaleMode.ScaleToFit);
        }

        private void DrawUserNotes()
        {
            float contentWidth = GetDetailsContentWidth();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Reproduction", EditorStyles.boldLabel);
            reproductionSteps = EditorGUILayout.TextField("Steps Summary", reproductionSteps, GUILayout.MaxWidth(contentWidth));
            EditorGUILayout.LabelField("Expected Result");
            expectedResult = EditorGUILayout.TextArea(expectedResult ?? string.Empty, GUILayout.MaxWidth(contentWidth), GUILayout.MinHeight(44f));
            EditorGUILayout.LabelField("Actual Result");
            actualResult = EditorGUILayout.TextArea(actualResult ?? string.Empty, GUILayout.MaxWidth(contentWidth), GUILayout.MinHeight(44f));
            EditorGUILayout.LabelField("Notes");
            notes = EditorGUILayout.TextArea(notes ?? string.Empty, GUILayout.MaxWidth(contentWidth), GUILayout.MinHeight(44f));
        }

        private void DrawPrivacyPreview()
        {
            EditorGUILayout.Space(8f);
            privacyPreviewFoldout = EditorGUILayout.Foldout(privacyPreviewFoldout, "Privacy Masked Preview", true);
            if (!privacyPreviewFoldout)
            {
                return;
            }

            string json = BugShotAIReportFormatter.ToJson(selectedReport);
            string preview = BugShotAIPrivacySanitizer.Sanitize(json, BugShotAIPrivacyOptions.FromSettings(settings));
            EditorGUILayout.TextArea(preview, GUILayout.MaxWidth(GetDetailsContentWidth()), GUILayout.MinHeight(120f));
        }

        private float GetDetailsContentWidth()
        {
            return Mathf.Max(280f, position.width - 280f);
        }

        private void RefreshReports()
        {
            reports.Clear();

            if (settings == null)
            {
                settings = BugShotAIEditorSettingsUtility.LoadSettings();
            }

            string rootPath = GetReportsRootPath();
            AddReportsFromRoot(rootPath, true);

            string legacyRoot = Path.Combine(Application.persistentDataPath, BugShotAIConstants.LegacyReportsFolderName);
            if (!string.Equals(Path.GetFullPath(rootPath), Path.GetFullPath(legacyRoot), StringComparison.OrdinalIgnoreCase))
            {
                AddReportsFromRoot(legacyRoot, true);
            }

            reports.Sort((left, right) =>
                GetReportWriteTimeUtc(right).CompareTo(GetReportWriteTimeUtc(left)));

            if (selectedReportIndex >= reports.Count)
            {
                selectedReportIndex = reports.Count - 1;
            }

            if (selectedReportIndex >= 0)
            {
                SelectReport(selectedReportIndex);
            }
            else if (reports.Count > 0)
            {
                SelectReport(0);
            }
            else
            {
                selectedReport = null;
                DestroyScreenshotPreview();
            }

            Repaint();
        }

        private void AddReportsFromRoot(string rootPath, bool includeLegacyFlatReports)
        {
            try
            {
                BugShotAIReportStorage storage = new BugShotAIReportStorage(rootPath);
                reports.AddRange(storage.FindReports(includeLegacyFlatReports));
            }
            catch (Exception ex)
            {
                BugShotAIEditorSettingsUtility.LogWarning($"Failed to read reports from {BugShotAIEditorSettingsUtility.BuildSafePathLabel(rootPath, false)}. {ex.Message}");
            }
        }

        private void SelectReport(int index)
        {
            if (index < 0 || index >= reports.Count)
            {
                selectedReportIndex = -1;
                selectedReport = null;
                DestroyScreenshotPreview();
                return;
            }

            selectedReportIndex = index;
            BugShotAIReportSummary summary = reports[index];
            selectedReport = new BugShotAIReportStorage(GetReportsRootPath()).LoadReport(summary.jsonPath);
            LoadUserNotes(selectedReport);
            LoadScreenshotPreview(summary.screenshotPath);
        }

        private void LoadUserNotes(BugShotAIReport report)
        {
            reproductionSteps = report?.userNotes?.reproductionSteps;
            expectedResult = report?.userNotes?.expectedResult;
            actualResult = report?.userNotes?.actualResult;
            notes = report?.userNotes?.notes;
        }

        private void LoadScreenshotPreview(string path)
        {
            DestroyScreenshotPreview();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, bytes))
                {
                    screenshotPreview = texture;
                }
                else
                {
                    DestroyImmediate(texture);
                }
            }
            catch (Exception ex)
            {
                BugShotAIEditorSettingsUtility.LogWarning($"Failed to load screenshot preview. {ex.Message}");
            }
        }

        private void DestroyScreenshotPreview()
        {
            if (screenshotPreview != null)
            {
                DestroyImmediate(screenshotPreview);
                screenshotPreview = null;
            }
        }

        private void OpenReportsFolder()
        {
            string rootPath = GetReportsRootPath();
            Directory.CreateDirectory(rootPath);
            EditorUtility.RevealInFinder(rootPath);
        }

        private void OpenJson(BugShotAIReportSummary summary)
        {
            if (summary == null || string.IsNullOrEmpty(summary.jsonPath) || !File.Exists(summary.jsonPath))
            {
                BugShotAIEditorSettingsUtility.LogWarning("No JSON report exists to open.");
                return;
            }

            EditorUtility.OpenWithDefaultApp(summary.jsonPath);
        }

        private void OpenReportFolder(BugShotAIReportSummary summary)
        {
            if (summary == null || string.IsNullOrEmpty(summary.reportDirectoryPath) || !Directory.Exists(summary.reportDirectoryPath))
            {
                BugShotAIEditorSettingsUtility.LogWarning("No report folder exists to open.");
                return;
            }

            EditorUtility.RevealInFinder(summary.reportDirectoryPath);
        }

        private void CopyJsonPath(BugShotAIReportSummary summary)
        {
            if (summary == null || string.IsNullOrEmpty(summary.jsonPath) || !File.Exists(summary.jsonPath))
            {
                BugShotAIEditorSettingsUtility.LogWarning("No JSON report path exists to copy.");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = summary.jsonPath;
            BugShotAIEditorSettingsUtility.Log("Copied full JSON report path.");
        }

        private void CopyPrompt(BugShotAIPromptLanguage language)
        {
            if (!TryLoadSelectedReportJson(out BugShotAIReport report, out string json))
            {
                return;
            }

            string prompt = BugShotAIReportFormatter.BuildPrompt(report, json, language);
            EditorGUIUtility.systemCopyBuffer = prompt;
            BugShotAIEditorSettingsUtility.Log(language == BugShotAIPromptLanguage.Japanese
                ? "Copied Japanese GitHub Issue prompt."
                : "Copied English GitHub Issue prompt.");
        }

        private void CopyMarkdown()
        {
            if (selectedReport == null)
            {
                BugShotAIEditorSettingsUtility.LogWarning("No report is selected.");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = BugShotAIReportFormatter.BuildMarkdown(selectedReport);
            BugShotAIEditorSettingsUtility.Log("Copied Markdown report.");
        }

        private void SaveSelectedReport()
        {
            BugShotAIReportSummary summary = GetSelectedOrLatestReport();
            if (selectedReport == null || summary == null)
            {
                BugShotAIEditorSettingsUtility.LogWarning("No report is selected.");
                return;
            }

            selectedReport.userNotes = new BugShotAIUserNotes
            {
                reproductionSteps = BugShotAITextUtility.NullIfEmpty(reproductionSteps),
                expectedResult = BugShotAITextUtility.NullIfEmpty(expectedResult),
                actualResult = BugShotAITextUtility.NullIfEmpty(actualResult),
                notes = BugShotAITextUtility.NullIfEmpty(notes)
            };

            try
            {
                BugShotAIReportStorage storage = new BugShotAIReportStorage(GetReportsRootPath());
                storage.SaveExistingReport(selectedReport, summary.jsonPath, settings);
                BugShotAIEditorSettingsUtility.Log("Saved sanitized JSON, Markdown, and prompts.");
                RefreshReports();
            }
            catch (Exception ex)
            {
                BugShotAIEditorSettingsUtility.LogWarning($"Failed to save report. {ex.Message}");
            }
        }

        private void DeleteSelectedReport()
        {
            BugShotAIReportSummary summary = GetSelectedOrLatestReport();
            if (summary == null)
            {
                return;
            }

            string label = BugShotAIEditorSettingsUtility.BuildSafePathLabel(summary.jsonPath, true);
            if (!EditorUtility.DisplayDialog("Delete BugShot Report", $"Delete {label}?", "Delete", "Cancel"))
            {
                return;
            }

            DeleteReport(summary);
            selectedReportIndex = -1;
            selectedReport = null;
            RefreshReports();
        }

        private void DeleteAllReports()
        {
            if (!EditorUtility.DisplayDialog("Delete All BugShot Reports", "Delete all BugShot reports shown in this window?", "Delete All", "Cancel"))
            {
                return;
            }

            foreach (BugShotAIReportSummary summary in reports.ToArray())
            {
                DeleteReport(summary);
            }

            selectedReportIndex = -1;
            selectedReport = null;
            RefreshReports();
        }

        private void DeleteReport(BugShotAIReportSummary summary)
        {
            BugShotAIReportStorage storage = new BugShotAIReportStorage(GetReportsRootPath());
            if (storage.DeleteReport(summary, out string error))
            {
                BugShotAIEditorSettingsUtility.Log("Deleted report.");
            }
            else
            {
                BugShotAIEditorSettingsUtility.LogWarning($"Failed to delete report. {error}");
            }
        }

        private void CreateRecorderInScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                BugShotAIEditorSettingsUtility.LogWarning("Cannot create recorder because there is no valid active scene.");
                return;
            }

            BugShotAIRecorder recorder = FindRecorderInScene(activeScene);
            if (recorder != null)
            {
                Selection.activeGameObject = recorder.gameObject;
                BugShotAIEditorSettingsUtility.Log("Found existing BugShotAIRecorder in the active scene. No new GameObject was created.");
                return;
            }

            GameObject recorderObject = new GameObject(RecorderObjectName);
            Undo.RegisterCreatedObjectUndo(recorderObject, "Create BugShotAI Recorder");
            Undo.AddComponent<BugShotAIRecorder>(recorderObject);
            Selection.activeGameObject = recorderObject;
            EditorSceneManager.MarkSceneDirty(activeScene);
            BugShotAIEditorSettingsUtility.Log("Created BugShotAI GameObject and added BugShotAIRecorder in the active scene.");
        }

        private void TriggerTestError()
        {
            StopReportPoll();
            reportPollPreviousPath = GetLastReportPath();
            BugShotAIEventLogger.Record("EditorWindow", "Triggered test error from BugShot AI Window");
            Debug.LogError("BugShot AI test error triggered from editor window.");

            reportPollDeadline = EditorApplication.timeSinceStartup + 5d;
            reportPollCallback = PollForTriggeredReport;
            EditorApplication.update += reportPollCallback;
        }

        private void PollForTriggeredReport()
        {
            string lastPath = GetLastReportPath();
            if (!string.IsNullOrEmpty(lastPath)
                && lastPath != reportPollPreviousPath
                && File.Exists(lastPath))
            {
                StopReportPoll();
                selectedReportIndex = -1;
                RefreshReports();
                BugShotAIEditorSettingsUtility.Log($"Last report saved: {BugShotAIEditorSettingsUtility.BuildSafePathLabel(lastPath, true)}");
                return;
            }

            if (EditorApplication.timeSinceStartup < reportPollDeadline)
            {
                return;
            }

            StopReportPoll();
            BugShotAIEditorSettingsUtility.LogWarning("Triggered test error, but no new JSON report path became available.");
        }

        private void StopReportPoll()
        {
            if (reportPollCallback == null)
            {
                return;
            }

            EditorApplication.update -= reportPollCallback;
            reportPollCallback = null;
        }

        private bool TryLoadSelectedReportJson(out BugShotAIReport report, out string json)
        {
            report = selectedReport;
            json = null;
            BugShotAIReportSummary summary = GetSelectedOrLatestReport();

            if (summary == null || string.IsNullOrEmpty(summary.jsonPath) || !File.Exists(summary.jsonPath))
            {
                BugShotAIEditorSettingsUtility.LogWarning("No report JSON is available.");
                return false;
            }

            json = File.ReadAllText(summary.jsonPath);
            if (report == null)
            {
                report = BugShotAIReportFormatter.FromJson(json);
            }

            if (report == null)
            {
                BugShotAIEditorSettingsUtility.LogWarning("Report JSON could not be parsed.");
                return false;
            }

            return true;
        }

        private BugShotAIReportSummary GetSelectedOrLatestReport()
        {
            if (selectedReportIndex >= 0 && selectedReportIndex < reports.Count)
            {
                return reports[selectedReportIndex];
            }

            return reports.Count > 0 ? reports[0] : null;
        }

        private string GetLastReportPath()
        {
            BugShotAIRecorder recorder = FindRecorderInActiveScene();
            if (recorder != null && !string.IsNullOrEmpty(recorder.LastReportPath) && File.Exists(recorder.LastReportPath))
            {
                return recorder.LastReportPath;
            }

            return reports.Count > 0 ? reports[0].jsonPath : null;
        }

        private string GetReportsRootPath()
        {
            return BugShotAIPathUtility.GetReportsRootPath(settings);
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

        private static string GetActiveSceneName()
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.name) ? "(Untitled Scene)" : scene.name;
        }

        private static string BuildShortCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return "Unknown";
            }

            return condition.Length <= 24 ? condition : condition.Substring(0, 21) + "...";
        }

        private static string BuildReportListLabel(BugShotAIReportSummary summary)
        {
            string timestamp = BugShotAITextUtility.UnknownIfEmpty(summary?.timestampUtc);
            if (DateTime.TryParse(timestamp, out DateTime parsedTimestamp))
            {
                timestamp = parsedTimestamp.ToUniversalTime().ToString("MM-dd HH:mm:ss 'UTC'");
            }

            return $"{BugShotAITextUtility.UnknownIfEmpty(summary?.logType)}\n{timestamp}\n{BuildShortCondition(summary?.condition)}";
        }

        private static DateTime GetReportWriteTimeUtc(BugShotAIReportSummary summary)
        {
            return summary != null && !string.IsNullOrEmpty(summary.jsonPath) && File.Exists(summary.jsonPath)
                ? File.GetLastWriteTimeUtc(summary.jsonPath)
                : DateTime.MinValue;
        }
    }
}
