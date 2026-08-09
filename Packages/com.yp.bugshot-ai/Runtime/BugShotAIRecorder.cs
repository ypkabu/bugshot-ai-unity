using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YP.BugShotAI
{
    [DisallowMultipleComponent]
    public sealed class BugShotAIRecorder : MonoBehaviour
    {
        private const int MaxPendingCaptureCount = 64;

        [Header("Target")]
        [Tooltip("Optional. Assign the player Transform to include its position in reports.")]
        [SerializeField] private Transform playerTransform;

        [Header("Settings")]
        [SerializeField] private BugShotAISettings settings = BugShotAISettings.CreateDefault();

        [Header("Default User Notes")]
        [SerializeField] private string reproductionSteps;
        [SerializeField] private string expectedResult;
        [SerializeField] private string actualResult;
        [SerializeField] private string notes;

        private readonly Queue<BugShotAIEvent> recentEvents = new Queue<BugShotAIEvent>();
        private readonly Queue<PendingCapture> pendingCaptures = new Queue<PendingCapture>();
        private readonly BugShotAIDuplicateTracker duplicateTracker = new BugShotAIDuplicateTracker();
        private BugShotAILogRingBuffer logRingBuffer = new BugShotAILogRingBuffer(80);
        private Coroutine captureCoroutine;
        private float smoothedFps;
        private bool isHandlingLog;
        private bool isQuitting;

        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        public BugShotAISettings Settings => settings;
        public string LastReportPath { get; private set; }
        public string LastReportFolderPath { get; private set; }

        private void OnValidate()
        {
            if (settings == null)
            {
                settings = BugShotAISettings.CreateDefault();
            }

            settings.Validate();
        }

        private void OnEnable()
        {
            if (settings == null)
            {
                settings = BugShotAISettings.CreateDefault();
            }

            settings.Validate();
            logRingBuffer = new BugShotAILogRingBuffer(settings.maxRecentLogs);

            BugShotAIEventLogger.EventRecorded -= RecordEvent;
            BugShotAIEventLogger.EventRecorded += RecordEvent;
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
            RecordEvent("BugShotAI", "Recorder enabled");
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            BugShotAIEventLogger.EventRecorded -= RecordEvent;

            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }

            if (isQuitting)
            {
                pendingCaptures.Clear();
            }
            else
            {
                FlushPendingCaptures("Screenshot capture was canceled because the Recorder was disabled.");
            }
        }

        public void FlushPendingCapturesBeforePlayModeExit()
        {
            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }

            FlushPendingCaptures("Screenshot capture was canceled because Play Mode was exiting.");
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void Update()
        {
            if (Time.unscaledDeltaTime <= 0f)
            {
                return;
            }

            float currentFps = 1f / Time.unscaledDeltaTime;
            smoothedFps = smoothedFps <= 0f
                ? currentFps
                : Mathf.Lerp(smoothedFps, currentFps, 0.05f);
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            BugShotAISettings activeSettings = ResolveSettings();
            logRingBuffer.SetMaxLogs(activeSettings.maxRecentLogs);
            logRingBuffer.Record(condition, stackTrace, type, activeSettings);

            if (isQuitting || isHandlingLog || BugShotAITextUtility.StartsWithInternalPrefix(condition))
            {
                return;
            }

            if (!activeSettings.ShouldCapture(type))
            {
                return;
            }

            string fingerprint = BugShotAIFingerprint.Generate(type.ToString(), condition, stackTrace);
            BugShotAIDuplicateResult duplicateResult = duplicateTracker.Register(
                fingerprint,
                Time.realtimeSinceStartup,
                DateTime.UtcNow,
                activeSettings.duplicateCooldownSeconds);

            if (!duplicateResult.ShouldCapture)
            {
                return;
            }

            isHandlingLog = true;

            try
            {
                PendingCapture pendingCapture = new PendingCapture(
                    condition,
                    stackTrace,
                    type,
                    activeSettings.Clone(),
                    duplicateResult);

                if (ShouldWaitForRenderedFrame(activeSettings))
                {
                    if (pendingCaptures.Count >= MaxPendingCaptureCount)
                    {
                        SavePendingCapture(
                            pendingCaptures.Dequeue(),
                            BugShotAIScreenshotResult.NotCaptured(
                                "Screenshot was skipped because the pending capture queue reached its limit."));
                    }

                    pendingCaptures.Enqueue(pendingCapture);
                    if (captureCoroutine == null)
                    {
                        captureCoroutine = StartCoroutine(ProcessPendingCaptures());
                    }
                }
                else
                {
                    SavePendingCapture(pendingCapture, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BugShotAIConstants.LogPrefix} Failed to capture report. {ex}");
            }
            finally
            {
                isHandlingLog = false;
            }
        }

        private static bool ShouldWaitForRenderedFrame(BugShotAISettings activeSettings)
        {
            return activeSettings != null
                   && activeSettings.captureScreenshots
                   && Application.isPlaying
                   && !Application.isBatchMode;
        }

        private IEnumerator ProcessPendingCaptures()
        {
            while (pendingCaptures.Count > 0)
            {
                // IMGUI and log callbacks can run with a non-Game render target bound.
                yield return new WaitForEndOfFrame();

                PendingCapture pendingCapture = pendingCaptures.Dequeue();

                if (!isActiveAndEnabled || isQuitting || !Application.isPlaying)
                {
                    SavePendingCapture(
                        pendingCapture,
                        BugShotAIScreenshotResult.NotCaptured(
                            "Screenshot capture was canceled before the rendered frame completed."));
                    continue;
                }

                SavePendingCapture(
                    pendingCapture,
                    BugShotAIScreenshotCaptureService.CapturePng(pendingCapture.Settings));
            }

            captureCoroutine = null;
        }

        private void FlushPendingCaptures(string screenshotError)
        {
            while (pendingCaptures.Count > 0)
            {
                SavePendingCapture(
                    pendingCaptures.Dequeue(),
                    BugShotAIScreenshotResult.NotCaptured(screenshotError));
            }
        }

        private void SavePendingCapture(PendingCapture pendingCapture, BugShotAIScreenshotResult screenshotResult)
        {
            bool previousHandlingState = isHandlingLog;
            isHandlingLog = true;

            try
            {
                CaptureBugReport(
                    pendingCapture.Condition,
                    pendingCapture.StackTrace,
                    pendingCapture.LogType,
                    pendingCapture.Settings,
                    pendingCapture.DuplicateResult,
                    screenshotResult);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{BugShotAIConstants.LogPrefix} Failed to capture report. {ex}");
            }
            finally
            {
                isHandlingLog = previousHandlingState;
            }
        }

        private void CaptureBugReport(
            string condition,
            string stackTrace,
            LogType type,
            BugShotAISettings activeSettings,
            BugShotAIDuplicateResult duplicateResult,
            BugShotAIScreenshotResult screenshotResult)
        {
            if (screenshotResult == null)
            {
                screenshotResult = BugShotAIScreenshotCaptureService.CapturePng(activeSettings);
            }

            BugShotAIReport report = BugShotAIReportBuilder.Build(new BugShotAIReportBuildContext
            {
                UtcNow = DateTime.UtcNow,
                Settings = activeSettings,
                Condition = condition,
                StackTrace = stackTrace,
                LogType = type,
                Fps = smoothedFps,
                PlayerTransform = playerTransform,
                OccurrenceCount = duplicateResult.OccurrenceCount,
                FirstOccurrenceUtc = duplicateResult.FirstOccurrenceUtc,
                ScreenshotError = screenshotResult.Error,
                ReproductionSteps = reproductionSteps,
                ExpectedResult = expectedResult,
                ActualResult = string.IsNullOrWhiteSpace(actualResult) ? condition : actualResult,
                Notes = notes,
                RecentEvents = recentEvents.ToArray(),
                RecentLogs = logRingBuffer.Snapshot()
            });

            BugShotAIPrivacySanitizer.SanitizeInPlace(report, activeSettings);

            string rootPath = BugShotAIPathUtility.GetReportsRootPath(activeSettings);
            BugShotAIReportStorage storage = new BugShotAIReportStorage(rootPath);
            BugShotAISaveResult saveResult = storage.Save(report, screenshotResult.PngBytes, activeSettings);

            LastReportPath = saveResult.jsonPath;
            LastReportFolderPath = saveResult.reportDirectoryPath;
            LogCleanupResult(saveResult);
        }

        private BugShotAISettings ResolveSettings()
        {
            BugShotAISettings activeSettings = BugShotAISettingsFile.LoadOrDefault(settings);
            settings = activeSettings.Clone();
            return activeSettings;
        }

        private void LogCleanupResult(BugShotAISaveResult saveResult)
        {
            if (saveResult == null)
            {
                return;
            }

            if (saveResult.cleanupErrors != null)
            {
                for (int i = 0; i < saveResult.cleanupErrors.Length; i++)
                {
                    Debug.LogWarning($"{BugShotAIConstants.LogPrefix} Report cleanup failed: {saveResult.cleanupErrors[i]}");
                }
            }

            if (saveResult.deletedReportPaths != null)
            {
                for (int i = 0; i < saveResult.deletedReportPaths.Length; i++)
                {
                    Debug.Log($"{BugShotAIConstants.LogPrefix} Deleted old report: {saveResult.deletedReportPaths[i]}");
                }
            }
        }

        private void RecordEvent(string category, string message)
        {
            BugShotAISettings activeSettings = settings ?? BugShotAISettings.CreateDefault();

            if (string.IsNullOrWhiteSpace(category))
            {
                category = "General";
            }

            recentEvents.Enqueue(new BugShotAIEvent
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                timeSinceStartup = Time.realtimeSinceStartup,
                category = category,
                message = message
            });

            while (recentEvents.Count > activeSettings.maxRecentEvents)
            {
                recentEvents.Dequeue();
            }
        }

        private sealed class PendingCapture
        {
            public PendingCapture(
                string condition,
                string stackTrace,
                LogType logType,
                BugShotAISettings settings,
                BugShotAIDuplicateResult duplicateResult)
            {
                Condition = condition;
                StackTrace = stackTrace;
                LogType = logType;
                Settings = settings;
                DuplicateResult = duplicateResult;
            }

            public string Condition { get; }
            public string StackTrace { get; }
            public LogType LogType { get; }
            public BugShotAISettings Settings { get; }
            public BugShotAIDuplicateResult DuplicateResult { get; }
        }
    }
}
