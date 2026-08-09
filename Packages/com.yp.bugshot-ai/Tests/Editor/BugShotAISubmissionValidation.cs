using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI.Tests
{
    public static class BugShotAISubmissionValidation
    {
        private const string ResultPathArgument = "-bugshotSubmissionResults";
        private const string StatePathArgument = "-bugshotSubmissionState";
        private const string ValidationRootName = "BugShotAISubmissionValidation";

        public static void RunAll()
        {
            RunAndExit("RunAll", ValidateRunAll);
        }

        public static void PersistencePhase1()
        {
            RunAndExit("PersistencePhase1", ValidatePersistencePhase1);
        }

        public static void PersistencePhase2()
        {
            RunAndExit("PersistencePhase2", ValidatePersistencePhase2);
        }

        private static void ValidateRunAll(ValidationContext context)
        {
            string root = CreateValidationDirectory("RunAll");
            context.Report.reportRootPath = BugShotAITextUtility.NormalizePath(root);

            ValidateSettings(context);
            ValidateLogCapture(context, root);
            ValidateFingerprintAndPrivacy(context);
            ValidateDuplicateSuppression(context, root);
            ValidateStorage(context, root);
            ValidateDomainReloadCallbackProtection(context, root);
            ValidateEditorWindowRegistration(context);
            ValidateDemoSample(context);
            ValidateScreenshotFallback(context, root);
        }

        private static void ValidateSettings(ValidationContext context)
        {
            BugShotAISettings settings = BugShotAISettings.CreateDefault();

            context.Expect(
                "Settings default retrieval",
                settings != null
                && settings.isEnabled
                && settings.automaticCaptureEnabled
                && settings.captureOnError
                && settings.captureOnException,
                "Default settings are enabled for Error and Exception capture.",
                "BugShotAISettings.CreateDefault()");

            context.Expect(
                "Automatic recording enabled",
                settings.ShouldCapture(LogType.Error) && settings.ShouldCapture(LogType.Exception),
                "ShouldCapture accepts Error and Exception with default settings.",
                "BugShotAISettings.ShouldCapture");

            settings.maxRecentEvents = 0;
            settings.duplicateCooldownSeconds = -1f;
            settings.outputFolderName = string.Empty;
            settings.Validate();

            context.Expect(
                "Settings validation clamps unsafe values",
                settings.maxRecentEvents == 1
                && Math.Abs(settings.duplicateCooldownSeconds) < 0.0001f
                && settings.outputFolderName == BugShotAIConstants.DefaultReportsFolderName,
                "OnValidate-compatible settings validation clamps minimums and restores default folder name.",
                "BugShotAISettings.Validate");
        }

        private static void ValidateLogCapture(ValidationContext context, string root)
        {
            string errorRoot = Path.Combine(root, "DebugLogError");
            CapturedReport errorCapture = CaptureWithRecorder(
                errorRoot,
                false,
                "BugShot AI submission validation Debug.LogError capture.");

            context.Expect(
                "Debug.LogError detection",
                errorCapture.Report != null && errorCapture.Report.condition.Contains("Debug.LogError capture"),
                "Recorder captured Debug.LogError through Application.logMessageReceived.",
                errorCapture.JsonPath);

            context.Expect(
                "Report ID generation",
                errorCapture.Report != null
                && !string.IsNullOrEmpty(errorCapture.Report.reportId)
                && !string.IsNullOrEmpty(errorCapture.Report.fingerprint),
                "Generated reportId and fingerprint are present.",
                errorCapture.JsonPath);

            context.Expect("report.json generation", File.Exists(errorCapture.JsonPath), "JSON report was saved.", errorCapture.JsonPath);
            context.Expect("report.md generation", File.Exists(errorCapture.MarkdownPath), "Markdown report was saved.", errorCapture.MarkdownPath);
            context.Expect("prompt_ja.txt generation", File.Exists(errorCapture.PromptJaPath), "Japanese prompt was saved.", errorCapture.PromptJaPath);
            context.Expect("prompt_en.txt generation", File.Exists(errorCapture.PromptEnPath), "English prompt was saved.", errorCapture.PromptEnPath);

            string exceptionRoot = Path.Combine(root, "NullReferenceException");
            CapturedReport exceptionCapture = CaptureWithRecorder(
                exceptionRoot,
                true,
                "BugShot AI submission validation NullReferenceException capture.");

            context.Expect(
                "NullReferenceException log detection",
                exceptionCapture.Report != null
                && exceptionCapture.Report.logType == LogType.Exception.ToString()
                && exceptionCapture.Report.condition.Contains("NullReferenceException"),
                "Recorder captured a caught NullReferenceException logged via Debug.LogException.",
                exceptionCapture.JsonPath);
        }

        private static void ValidateFingerprintAndPrivacy(ValidationContext context)
        {
            string first = BugShotAIFingerprint.Generate("Error", "same condition", "Demo:Run()");
            string second = BugShotAIFingerprint.Generate("Error", "same condition", "Demo:Run()");

            context.Expect(
                "Fingerprint generation",
                first == second && first.Length == 16,
                "Fingerprint is stable and uses the expected short length.",
                first);

            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.maskIpAddresses = true;
            string raw = @"C:\Users\demo-user\Secret\Player.cs /Users/demo-user/Secret /home/demo-user/Secret \\BuildShare\Users\demo-user\Secret demo-user@example.invalid Authorization: Bearer demo-secret api_key=demo-api-key https://example.invalid/callback?token=query-secret#access_token=fragment-secret 192.168.10.24";
            string sanitized = BugShotAIPrivacySanitizer.Sanitize(raw, BugShotAIPrivacyOptions.FromSettings(settings));

            bool masked = !sanitized.Contains("demo-user")
                          && !sanitized.Contains("demo-secret")
                          && !sanitized.Contains("demo-api-key")
                          && !sanitized.Contains("query-secret")
                          && !sanitized.Contains("fragment-secret")
                          && !sanitized.Contains("192.168.10.24")
                          && sanitized.Contains("<USER_HOME>")
                          && sanitized.Contains("<REDACTED>")
                          && sanitized.Contains("<IP_ADDRESS>");

            context.Expect(
                "Privacy Sanitizer applied",
                masked,
                "Dummy paths, email, Authorization, token-like values, URL secrets, and IP were masked.",
                sanitized);
        }

        private static void ValidateDuplicateSuppression(ValidationContext context, string root)
        {
            string duplicateRoot = Path.Combine(root, "DuplicateSuppression");
            Directory.CreateDirectory(duplicateRoot);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject recorderObject = new GameObject("BugShotAI");
            BugShotAIRecorder recorder = recorderObject.AddComponent<BugShotAIRecorder>();
            ConfigureRecorder(recorder, duplicateRoot, false);
            recorder.Settings.duplicateCooldownSeconds = 60f;
            InvokeRecorderLifecycle(recorder, "OnEnable");

            string message = "BugShot AI duplicate suppression validation error.";
            for (int i = 0; i < 5; i++)
            {
                Debug.LogError(message);
            }

            string latestJson = WaitForLatestReportJson(duplicateRoot, 1);
            int reportCount = CountReportJsonFiles(duplicateRoot);
            InvokeRecorderLifecycle(recorder, "OnDisable");
            UnityEngine.Object.DestroyImmediate(recorderObject);

            context.Expect(
                "Duplicate error suppression",
                reportCount == 1 && !string.IsNullOrEmpty(latestJson),
                "Five identical Debug.LogError calls during cooldown produced one saved report.",
                duplicateRoot);

            BugShotAIDuplicateTracker tracker = new BugShotAIDuplicateTracker();
            BugShotAIDuplicateResult first = tracker.Register("same", 1f, DateTime.UtcNow, 60f);
            BugShotAIDuplicateResult second = tracker.Register("same", 2f, DateTime.UtcNow, 60f);
            BugShotAIDuplicateResult third = tracker.Register("same", 3f, DateTime.UtcNow, 60f);

            context.Expect(
                "Duplicate occurrence count aggregation",
                first.OccurrenceCount == 1
                && !second.ShouldCapture
                && !third.ShouldCapture
                && third.OccurrenceCount == 3,
                "DuplicateTracker counts suppressed occurrences even when reports are not saved.",
                "BugShotAIDuplicateTracker.Register");
        }

        private static void ValidateStorage(ValidationContext context, string root)
        {
            string storageRoot = Path.Combine(root, "Storage");
            BugShotAISettings settings = CreateValidationSettings(storageRoot, false);
            BugShotAIReportStorage storage = new BugShotAIReportStorage(storageRoot);
            BugShotAISaveResult saved = storage.Save(CreateReport("storage-created"), null, settings);

            context.Expect(
                "Save root creation when missing",
                Directory.Exists(storageRoot) && File.Exists(saved.jsonPath),
                "Storage created the missing root and report folder.",
                saved.jsonPath);

            List<BugShotAIReportSummary> history = storage.FindReports(false);
            context.Expect(
                "Report history loading",
                history.Any(report => report.reportId == "storage-created"),
                "FindReports loaded the saved report summary.",
                storageRoot);

            BugShotAIReportSummary summary = history.FirstOrDefault(report => report.reportId == "storage-created");
            bool deleted = storage.DeleteReport(summary, out string deleteError);
            context.Expect(
                "Report deletion",
                deleted && summary != null && !Directory.Exists(summary.reportDirectoryPath),
                "DeleteReport removed the report folder.",
                string.IsNullOrEmpty(deleteError) ? storageRoot : deleteError);

            string corruptDirectory = Path.Combine(storageRoot, "corrupt");
            Directory.CreateDirectory(corruptDirectory);
            File.WriteAllText(Path.Combine(corruptDirectory, "report.json"), "{ broken json");
            List<BugShotAIReportSummary> reportsAfterCorrupt = storage.FindReports(false);
            context.Expect(
                "Corrupt report.json skip",
                reportsAfterCorrupt.All(report => report.reportId != "corrupt"),
                "Corrupt report.json was skipped without throwing.",
                corruptDirectory);

            string countRoot = Path.Combine(root, "MaxCount");
            BugShotAISettings countSettings = CreateValidationSettings(countRoot, false);
            countSettings.maxReportCount = 2;
            BugShotAIReportStorage countStorage = new BugShotAIReportStorage(countRoot);
            countStorage.Save(CreateReport("count-1"), null, countSettings);
            Thread.Sleep(20);
            countStorage.Save(CreateReport("count-2"), null, countSettings);
            Thread.Sleep(20);
            countStorage.Save(CreateReport("count-3"), null, countSettings);

            context.Expect(
                "Max report count cleanup",
                CountReportJsonFiles(countRoot) <= 2,
                "Storage cleanup kept report count within maxReportCount.",
                countRoot);

            List<BugShotAIReportFolderInfo> sizeCandidates = new List<BugShotAIReportFolderInfo>
            {
                new BugShotAIReportFolderInfo { DirectoryPath = "old", LastWriteTimeUtc = new DateTime(2026, 1, 1), TotalBytes = 80 },
                new BugShotAIReportFolderInfo { DirectoryPath = "new", LastWriteTimeUtc = new DateTime(2026, 1, 2), TotalBytes = 80 }
            };
            List<BugShotAIReportFolderInfo> deleteTargets = BugShotAIStoragePolicy.SelectReportsToDelete(sizeCandidates, 10, 100);
            context.Expect(
                "Max storage size deletion target selection",
                deleteTargets.Count == 1 && deleteTargets[0].DirectoryPath == "old",
                "Storage policy selected the oldest report when capacity was exceeded.",
                "BugShotAIStoragePolicy.SelectReportsToDelete");

            string invalidRootParent = Path.Combine(root, "InvalidRoot");
            Directory.CreateDirectory(invalidRootParent);
            string invalidRoot = Path.Combine(invalidRootParent, "not-a-directory");
            File.WriteAllText(invalidRoot, "x");
            bool failedSafely = false;
            try
            {
                new BugShotAIReportStorage(invalidRoot).Save(CreateReport("invalid-root"), null, CreateValidationSettings(invalidRoot, false));
            }
            catch (IOException)
            {
                failedSafely = true;
            }
            catch (UnauthorizedAccessException)
            {
                failedSafely = true;
            }

            context.Expect(
                "Invalid output root safe failure",
                failedSafely,
                "A file used as the output root fails before partial report output is produced.",
                invalidRoot);
        }

        private static void ValidateDomainReloadCallbackProtection(ValidationContext context, string root)
        {
            string domainRoot = Path.Combine(root, "DomainReloadCallbackSimulation");
            Directory.CreateDirectory(domainRoot);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject recorderObject = new GameObject("BugShotAI");
            BugShotAIRecorder recorder = recorderObject.AddComponent<BugShotAIRecorder>();
            ConfigureRecorder(recorder, domainRoot, false);

            InvokeRecorderLifecycle(recorder, "OnEnable");
            InvokeRecorderLifecycle(recorder, "OnDisable");
            InvokeRecorderLifecycle(recorder, "OnEnable");
            InvokeRecorderLifecycle(recorder, "OnDisable");
            InvokeRecorderLifecycle(recorder, "OnEnable");

            Debug.LogError("BugShot AI domain reload callback simulation error.");
            string latestJson = WaitForLatestReportJson(domainRoot, 1);
            int reportCount = CountReportJsonFiles(domainRoot);
            InvokeRecorderLifecycle(recorder, "OnDisable");
            UnityEngine.Object.DestroyImmediate(recorderObject);

            context.Expect(
                "Domain Reload callback duplicate prevention",
                reportCount == 1 && !string.IsNullOrEmpty(latestJson),
                "Simulated repeated OnEnable calls did not register duplicate log callbacks.",
                domainRoot);

            context.Note("Full script recompilation/domain reload was not forced in this CLI pass; callback duplication was validated by repeated OnEnable/OnDisable.");
        }

        private static void ValidateEditorWindowRegistration(ValidationContext context)
        {
            Type windowType = Type.GetType("YP.BugShotAI.Editor.BugShotAIWindow, yp.bugshot-ai.editor");
            MethodInfo openMethod = windowType == null
                ? null
                : windowType.GetMethod("OpenWindow", BindingFlags.Public | BindingFlags.Static);

            MenuItem menuItem = openMethod == null
                ? null
                : openMethod.GetCustomAttributes(typeof(MenuItem), false).OfType<MenuItem>().FirstOrDefault();

            string menuPath = GetMenuItemPath(menuItem);
            context.Expect(
                "Editor Window menu registration",
                menuPath == "Tools/BugShot AI/Open Window",
                "BugShotAIWindow.OpenWindow is registered in the Unity Tools menu.",
                string.IsNullOrEmpty(menuPath) ? "(missing)" : menuPath);
        }

        private static void ValidateDemoSample(ValidationContext context)
        {
            string packageRoot = GetPackageRootPath();
            string demoPanelPath = Path.Combine(packageRoot, "Samples~", "BasicSetup", "BugShotAIDemoErrorPanel.cs");
            bool fileExists = File.Exists(demoPanelPath);
            string source = fileExists ? File.ReadAllText(demoPanelPath) : string.Empty;

            bool hasExpectedButtons = source.Contains("NullReferenceException")
                                      && source.Contains("IndexOutOfRangeException")
                                      && source.Contains("Debug.LogError")
                                      && source.Contains("Long StackTrace Exception")
                                      && source.Contains("Duplicate Error Burst");

            bool isolated = BugShotAITextUtility.NormalizePath(demoPanelPath).Contains("/Samples~/BasicSetup/");
            bool noUnityEditorReference = !source.Contains("UnityEditor");
            bool duplicateBurstIsBounded = source.Contains("for (int i = 0; i < 5; i++)");
            bool explainsPurpose = source.Contains("Use these buttons in Play Mode to verify capture behavior.");

            context.Expect(
                "Demo Error Panel sample content",
                fileExists && hasExpectedButtons && isolated && noUnityEditorReference && duplicateBurstIsBounded && explainsPurpose,
                "Demo error triggers are isolated under Samples~, bounded, and visible to the user.",
                demoPanelPath);

            string demoBugTriggerPath = Path.Combine(packageRoot, "Samples~", "BasicSetup", "BugShotAIDemoBugTrigger.cs");
            string triggerSource = File.Exists(demoBugTriggerPath) ? File.ReadAllText(demoBugTriggerPath) : string.Empty;
            context.Expect(
                "Demo Bug Trigger input compatibility",
                triggerSource.Contains("ENABLE_LEGACY_INPUT_MANAGER")
                && triggerSource.Contains("ENABLE_INPUT_SYSTEM")
                && !triggerSource.Contains("UnityEditor"),
                "Demo trigger supports legacy and new input conditionals without UnityEditor references.",
                demoBugTriggerPath);

            context.Note("Sample import through the Package Manager UI is left for clean-project validation/human review; CLI verified sample definition files and isolation.");
        }

        private static void ValidateScreenshotFallback(ValidationContext context, string root)
        {
            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.captureScreenshots = true;
            BugShotAIScreenshotResult screenshotResult = BugShotAIScreenshotCaptureService.CapturePng(settings);

            context.Expect(
                "Batchmode screenshot capture does not throw",
                screenshotResult != null,
                screenshotResult != null && screenshotResult.HasScreenshot
                    ? "Screenshot bytes were captured in this environment."
                    : "No screenshot bytes were captured; capture failure is represented as data.",
                screenshotResult == null ? "(null)" : BugShotAITextUtility.UnknownIfEmpty(screenshotResult.Error));

            string screenshotRoot = Path.Combine(root, "ScreenshotFallback");
            CapturedReport capture = CaptureWithRecorder(
                screenshotRoot,
                false,
                "BugShot AI screenshot fallback validation error.",
                true);

            bool reportSaved = capture.Report != null && File.Exists(capture.JsonPath);
            bool screenshotOk = !string.IsNullOrEmpty(capture.Report?.screenshotFileName)
                                ? File.Exists(Path.Combine(capture.ReportDirectory, capture.Report.screenshotFileName))
                                : !string.IsNullOrEmpty(capture.Report?.screenshotError) || Application.isBatchMode;

            context.Expect(
                "Screenshot failure fallback preserves report",
                reportSaved && screenshotOk,
                "Report generation succeeded whether screenshot capture returned bytes or an error.",
                capture.JsonPath);
        }

        private static void ValidatePersistencePhase1(ValidationContext context)
        {
            string statePath = GetStatePath();
            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            string settingsPath = BugShotAISettingsFile.GetProjectSettingsPath();
            string persistenceRoot = Path.Combine(projectRoot, "Logs", ValidationRootName, "PersistenceReports");
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));
            Directory.CreateDirectory(persistenceRoot);

            BugShotAISubmissionPersistenceState state = new BugShotAISubmissionPersistenceState
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                settingsPath = BugShotAITextUtility.NormalizePath(settingsPath),
                hadPreviousSettings = File.Exists(settingsPath),
                previousSettingsJson = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null,
                expectedOutputRoot = BugShotAITextUtility.NormalizePath(persistenceRoot),
                expectedReportId = "restart-persistence-report"
            };

            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.customOutputDirectory = persistenceRoot;
            settings.captureScreenshots = false;
            settings.maskIpAddresses = true;
            BugShotAISettingsFile.Save(settings);

            BugShotAIReportStorage storage = new BugShotAIReportStorage(persistenceRoot);
            BugShotAISaveResult saveResult = storage.Save(CreateReport(state.expectedReportId), null, settings);
            state.expectedReportJsonPath = saveResult.jsonPath;

            File.WriteAllText(statePath, JsonUtility.ToJson(state, true));

            context.Expect(
                "Editor restart Phase 1 settings saved",
                File.Exists(settingsPath) && File.Exists(statePath),
                "Saved ProjectSettings/BugShotAISettings.json and persistence state for the next Unity launch.",
                statePath);

            context.Expect(
                "Editor restart Phase 1 report saved",
                File.Exists(saveResult.jsonPath),
                "Saved a report that Phase 2 must discover after Unity restart.",
                saveResult.jsonPath);
        }

        private static void ValidatePersistencePhase2(ValidationContext context)
        {
            string statePath = GetStatePath();
            if (!File.Exists(statePath))
            {
                context.Expect("Editor restart Phase 2 state exists", false, "Persistence state file is missing.", statePath);
                return;
            }

            BugShotAISubmissionPersistenceState state = JsonUtility.FromJson<BugShotAISubmissionPersistenceState>(File.ReadAllText(statePath));
            BugShotAISettings loaded = BugShotAISettingsFile.LoadOrDefault(BugShotAISettings.CreateDefault());
            string loadedRoot = BugShotAITextUtility.NormalizePath(BugShotAIPathUtility.GetReportsRootPath(loaded));

            context.Expect(
                "Editor restart Phase 2 settings persisted",
                string.Equals(loadedRoot, state.expectedOutputRoot, StringComparison.OrdinalIgnoreCase),
                "Project settings persisted across a new Unity process.",
                loadedRoot);

            BugShotAIReportStorage storage = new BugShotAIReportStorage(state.expectedOutputRoot);
            List<BugShotAIReportSummary> reports = storage.FindReports(false);
            context.Expect(
                "Editor restart Phase 2 history loaded",
                reports.Any(report => report.reportId == state.expectedReportId),
                "Report history was readable after a new Unity process.",
                state.expectedOutputRoot);

            RestoreSettings(state);
            TryDeleteFile(statePath);

            context.Expect(
                "Editor restart Phase 2 cleanup",
                !File.Exists(statePath),
                "Validation state file was removed after the persistence check.",
                statePath);
        }

        private static CapturedReport CaptureWithRecorder(string root, bool logException, string message, bool captureScreenshots = false)
        {
            Directory.CreateDirectory(root);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject recorderObject = new GameObject("BugShotAI");
            BugShotAIRecorder recorder = recorderObject.AddComponent<BugShotAIRecorder>();
            ConfigureRecorder(recorder, root, captureScreenshots);
            InvokeRecorderLifecycle(recorder, "OnEnable");

            BugShotAIEventLogger.Record("Validation", "Submission validation breadcrumb before capture");
            int expectedCount = CountReportJsonFiles(root) + 1;

            if (logException)
            {
                try
                {
                    CauseNullReferenceException();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            else
            {
                Debug.LogError(message);
            }

            string jsonPath = WaitForLatestReportJson(root, expectedCount);
            InvokeRecorderLifecycle(recorder, "OnDisable");
            UnityEngine.Object.DestroyImmediate(recorderObject);

            BugShotAIReport report = string.IsNullOrEmpty(jsonPath)
                ? null
                : new BugShotAIReportStorage(root).LoadReport(jsonPath);

            string reportDirectory = string.IsNullOrEmpty(jsonPath) ? null : Path.GetDirectoryName(jsonPath);
            return new CapturedReport
            {
                Report = report,
                JsonPath = BugShotAITextUtility.NormalizePath(jsonPath),
                ReportDirectory = BugShotAITextUtility.NormalizePath(reportDirectory),
                MarkdownPath = BugShotAITextUtility.NormalizePath(CombineIfNotEmpty(reportDirectory, "report.md")),
                PromptJaPath = BugShotAITextUtility.NormalizePath(CombineIfNotEmpty(reportDirectory, "prompt_ja.txt")),
                PromptEnPath = BugShotAITextUtility.NormalizePath(CombineIfNotEmpty(reportDirectory, "prompt_en.txt"))
            };
        }

        private static void ConfigureRecorder(BugShotAIRecorder recorder, string root, bool captureScreenshots)
        {
            BugShotAISettings settings = recorder.Settings;
            settings.isEnabled = true;
            settings.automaticCaptureEnabled = true;
            settings.captureOnError = true;
            settings.captureOnException = true;
            settings.captureOnAssert = true;
            settings.captureOnWarning = false;
            settings.captureScreenshots = captureScreenshots;
            settings.customOutputDirectory = root;
            settings.duplicateCooldownSeconds = 2f;
            settings.maxRecentEvents = 20;
            settings.maxRecentLogs = 20;
            settings.maxReportCount = 100;
            settings.maxStorageMegabytes = 100;
            settings.maxLogChars = 8000;
            settings.maxStackTraceChars = 12000;
            settings.maskEmailAddresses = true;
            settings.maskIpAddresses = true;
            settings.Validate();
        }

        private static void InvokeRecorderLifecycle(BugShotAIRecorder recorder, string methodName)
        {
            MethodInfo method = typeof(BugShotAIRecorder).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(recorder, null);
        }

        private static BugShotAISettings CreateValidationSettings(string root, bool captureScreenshots)
        {
            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.customOutputDirectory = root;
            settings.captureScreenshots = captureScreenshots;
            settings.maxReportCount = 100;
            settings.maxStorageMegabytes = 100;
            settings.maskIpAddresses = true;
            settings.Validate();
            return settings;
        }

        private static BugShotAIReport CreateReport(string reportId)
        {
            return new BugShotAIReport
            {
                schemaVersion = BugShotAIConstants.ReportSchemaVersion,
                reportId = reportId,
                fingerprint = BugShotAIFingerprint.Generate("Error", "validation", "SubmissionValidation:Run()"),
                occurrenceCount = 1,
                firstOccurrenceUtc = "2026-07-31T00:00:00Z",
                timestampUtc = DateTime.UtcNow.ToString("o"),
                sceneName = "SubmissionValidationScene",
                scenePath = "Assets/SubmissionValidationScene.unity",
                projectName = "BugShotAI Validation",
                logType = LogType.Error.ToString(),
                condition = "BugShot AI submission validation report.",
                stackTrace = "BugShotAISubmissionValidation:CreateReport()",
                fps = 60f,
                isPlaying = Application.isPlaying,
                editorState = BugShotAIReportBuilder.CollectEditorState(),
                userNotes = new BugShotAIUserNotes
                {
                    reproductionSteps = "Automated submission validation.",
                    expectedResult = "Report files are generated.",
                    actualResult = "Report files were generated.",
                    notes = "Generated by BugShotAISubmissionValidation."
                },
                environment = BugShotAIReportBuilder.CollectEnvironment(),
                playerPosition = new BugShotAIPlayerPosition
                {
                    hasPlayer = false
                },
                recentEvents = new[]
                {
                    new BugShotAIEvent
                    {
                        timestampUtc = DateTime.UtcNow.ToString("o"),
                        timeSinceStartup = Time.realtimeSinceStartup,
                        category = "Validation",
                        message = "Created storage validation report."
                    }
                },
                recentLogs = Array.Empty<BugShotAILogEntry>()
            };
        }

        private static string WaitForLatestReportJson(string root, int minimumCount)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            string latest = null;

            while (DateTime.UtcNow < deadline)
            {
                string[] paths = GetReportJsonFiles(root);
                if (paths.Length >= minimumCount)
                {
                    latest = paths
                        .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
                        .FirstOrDefault();
                    break;
                }

                Thread.Sleep(50);
            }

            return latest;
        }

        private static int CountReportJsonFiles(string root)
        {
            return GetReportJsonFiles(root).Length;
        }

        private static string[] GetReportJsonFiles(string root)
        {
            return Directory.Exists(root)
                ? Directory.GetFiles(root, "report.json", SearchOption.AllDirectories)
                : Array.Empty<string>();
        }

        private static string CreateValidationDirectory(string phase)
        {
            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string path = Path.Combine(projectRoot, "Temp", ValidationRootName, phase + "_" + stamp);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string GetPackageRootPath()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BugShotAIRecorder).Assembly);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return packageInfo.resolvedPath;
            }

            return Path.Combine(BugShotAIPathUtility.GetProjectRootPath(), "Packages", BugShotAIConstants.PackageName);
        }

        private static string GetMenuItemPath(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                return null;
            }

            Type menuItemType = typeof(MenuItem);
            PropertyInfo property = menuItemType.GetProperty("menuItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                return property.GetValue(menuItem) as string;
            }

            FieldInfo field = menuItemType.GetField("menuItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null ? null : field.GetValue(menuItem) as string;
        }

        private static void CauseNullReferenceException()
        {
            string value = null;
            value.ToString();
        }

        private static string CombineIfNotEmpty(string directory, string fileName)
        {
            return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, fileName);
        }

        private static string GetResultPath(string phase)
        {
            string explicitPath = GetArgumentValue(ResultPathArgument);
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return Path.GetFullPath(explicitPath);
            }

            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            return Path.Combine(projectRoot, "Logs", "BugShotAI_SubmissionValidation_" + phase + ".json");
        }

        private static string GetStatePath()
        {
            string explicitPath = GetArgumentValue(StatePathArgument);
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return Path.GetFullPath(explicitPath);
            }

            string projectRoot = BugShotAIPathUtility.GetProjectRootPath();
            return Path.Combine(projectRoot, "Temp", ValidationRootName, "persistence_state.json");
        }

        private static string GetArgumentValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void RestoreSettings(BugShotAISubmissionPersistenceState state)
        {
            if (state == null || string.IsNullOrEmpty(state.settingsPath))
            {
                return;
            }

            string settingsPath = state.settingsPath;
            string directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (state.hadPreviousSettings)
            {
                File.WriteAllText(settingsPath, state.previousSettingsJson ?? string.Empty);
            }
            else
            {
                TryDeleteFile(settingsPath);
            }

            AssetDatabase.Refresh();
        }

        private static void TryDeleteFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void RunAndExit(string phase, Action<ValidationContext> action)
        {
            string resultPath = GetResultPath(phase);
            ValidationContext context = new ValidationContext(phase, resultPath);

            try
            {
                action(context);
            }
            catch (Exception ex)
            {
                context.Expect("Unhandled validation exception", false, ex.ToString(), phase);
            }

            context.Save();
            Debug.Log($"{BugShotAIConstants.LogPrefix} Submission validation {phase} finished. Failed={context.FailedCount}. Results={resultPath}");
            EditorApplication.Exit(context.FailedCount > 0 ? 1 : 0);
        }

        private sealed class ValidationContext
        {
            private readonly List<BugShotAISubmissionValidationItem> items = new List<BugShotAISubmissionValidationItem>();
            private readonly List<string> notes = new List<string>();

            public ValidationContext(string phase, string resultPath)
            {
                Report = new BugShotAISubmissionValidationReport
                {
                    phase = phase,
                    timestampUtc = DateTime.UtcNow.ToString("o"),
                    projectPath = BugShotAITextUtility.NormalizePath(BugShotAIPathUtility.GetProjectRootPath()),
                    unityVersion = Application.unityVersion,
                    resultPath = BugShotAITextUtility.NormalizePath(resultPath)
                };
            }

            public BugShotAISubmissionValidationReport Report { get; }
            public int FailedCount { get; private set; }

            public void Expect(string name, bool passed, string details, string evidence)
            {
                if (!passed)
                {
                    FailedCount++;
                }

                items.Add(new BugShotAISubmissionValidationItem
                {
                    name = name,
                    status = passed ? "Pass" : "Fail",
                    details = details,
                    evidence = BugShotAITextUtility.NormalizePath(evidence)
                });
            }

            public void Note(string note)
            {
                notes.Add(note);
            }

            public void Save()
            {
                Report.failedCount = FailedCount;
                Report.totalCount = items.Count;
                Report.passedCount = items.Count - FailedCount;
                Report.items = items.ToArray();
                Report.notes = notes.ToArray();

                string directory = Path.GetDirectoryName(Report.resultPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(Report.resultPath, JsonUtility.ToJson(Report, true));
                File.WriteAllText(Path.ChangeExtension(Report.resultPath, ".md"), BuildMarkdown());
            }

            private string BuildMarkdown()
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                builder.AppendLine("# BugShot AI Submission Validation");
                builder.AppendLine();
                builder.AppendLine("- Phase: `" + Report.phase + "`");
                builder.AppendLine("- Timestamp UTC: `" + Report.timestampUtc + "`");
                builder.AppendLine("- Unity Version: `" + Report.unityVersion + "`");
                builder.AppendLine("- Project: `" + Report.projectPath + "`");
                builder.AppendLine("- Passed: `" + Report.passedCount + "/" + Report.totalCount + "`");
                builder.AppendLine();
                builder.AppendLine("| Item | Status | Details | Evidence |");
                builder.AppendLine("|---|---|---|---|");

                foreach (BugShotAISubmissionValidationItem item in items)
                {
                    builder.AppendLine("| " + EscapeTable(item.name) + " | " + item.status + " | " + EscapeTable(item.details) + " | `" + EscapeTable(item.evidence) + "` |");
                }

                if (notes.Count > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("## Notes");
                    foreach (string note in notes)
                    {
                        builder.AppendLine("- " + note);
                    }
                }

                return builder.ToString();
            }

            private static string EscapeTable(string value)
            {
                return string.IsNullOrEmpty(value)
                    ? string.Empty
                    : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
            }
        }

        [Serializable]
        private sealed class BugShotAISubmissionValidationReport
        {
            public string phase;
            public string timestampUtc;
            public string projectPath;
            public string unityVersion;
            public string resultPath;
            public string reportRootPath;
            public int totalCount;
            public int passedCount;
            public int failedCount;
            public BugShotAISubmissionValidationItem[] items;
            public string[] notes;
        }

        [Serializable]
        private sealed class BugShotAISubmissionValidationItem
        {
            public string name;
            public string status;
            public string details;
            public string evidence;
        }

        [Serializable]
        private sealed class BugShotAISubmissionPersistenceState
        {
            public string timestampUtc;
            public string settingsPath;
            public bool hadPreviousSettings;
            public string previousSettingsJson;
            public string expectedOutputRoot;
            public string expectedReportId;
            public string expectedReportJsonPath;
        }

        private sealed class CapturedReport
        {
            public BugShotAIReport Report;
            public string JsonPath;
            public string ReportDirectory;
            public string MarkdownPath;
            public string PromptJaPath;
            public string PromptEnPath;
        }
    }
}
