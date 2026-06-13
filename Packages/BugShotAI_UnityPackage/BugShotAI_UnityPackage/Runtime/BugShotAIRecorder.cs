using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI
{
    /// <summary>
    /// Minimal MVP recorder for BugShot AI.
    /// Attach this component to one GameObject in the first scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BugShotAIRecorder : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Optional. Assign the player Transform to include its position in reports.")]
        [SerializeField] private Transform playerTransform;

        [Header("Capture")]
        [SerializeField] private bool captureOnError = true;
        [SerializeField] private bool captureOnException = true;
        [SerializeField] private bool captureOnAssert = false;
        [SerializeField] private int maxRecentEvents = 50;
        [SerializeField] private float duplicateCooldownSeconds = 1.0f;
        [SerializeField] private string outputFolderName = "BugShotAI";

        private readonly Queue<BugShotAIEvent> recentEvents = new Queue<BugShotAIEvent>();
        private float smoothedFps;
        private float lastCaptureRealtime = -999f;
        private bool isHandlingLog;

        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        private void OnEnable()
        {
            BugShotAIEventLogger.EventRecorded += RecordEvent;
            Application.logMessageReceived += OnLogMessageReceived;
            RecordEvent("BugShotAI", "Recorder enabled");
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            BugShotAIEventLogger.EventRecorded -= RecordEvent;
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
            if (!ShouldCapture(type))
            {
                return;
            }

            if (isHandlingLog)
            {
                return;
            }

            if (Time.realtimeSinceStartup - lastCaptureRealtime < duplicateCooldownSeconds)
            {
                return;
            }

            isHandlingLog = true;
            lastCaptureRealtime = Time.realtimeSinceStartup;

            try
            {
                CaptureBugReport(condition, stackTrace, type);
            }
            catch (Exception ex)
            {
                // Avoid recursive Debug.LogError here. A plain console write is safer for a logger callback.
                Console.WriteLine($"[BugShotAI] Failed to capture bug report: {ex}");
            }
            finally
            {
                isHandlingLog = false;
            }
        }

        private bool ShouldCapture(LogType type)
        {
            return (type == LogType.Error && captureOnError)
                   || (type == LogType.Exception && captureOnException)
                   || (type == LogType.Assert && captureOnAssert);
        }

        private void CaptureBugReport(string condition, string stackTrace, LogType type)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string outputDirectory = Path.Combine(Application.persistentDataPath, outputFolderName);
            Directory.CreateDirectory(outputDirectory);

            string screenshotPath = Path.Combine(outputDirectory, $"bugshot_{timestamp}.png");
            ScreenCapture.CaptureScreenshot(screenshotPath);

            BugShotAIReport report = new BugShotAIReport
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                sceneName = SceneManager.GetActiveScene().name,
                logType = type.ToString(),
                condition = condition,
                stackTrace = stackTrace,
                screenshotPath = screenshotPath,
                fps = smoothedFps,
                playerPosition = CreatePlayerPosition(),
                recentEvents = recentEvents.ToArray()
            };

            string reportPath = Path.Combine(outputDirectory, $"bugshot_{timestamp}.json");
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(reportPath, json);
        }

        private BugShotAIPlayerPosition CreatePlayerPosition()
        {
            if (playerTransform == null)
            {
                return new BugShotAIPlayerPosition
                {
                    hasPlayer = false,
                    x = 0f,
                    y = 0f,
                    z = 0f
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

        private void RecordEvent(string category, string message)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "General";
            }

            recentEvents.Enqueue(new BugShotAIEvent
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                timeSinceStartup = Time.realtimeSinceStartup,
                category = category,
                message = message ?? string.Empty
            });

            while (recentEvents.Count > maxRecentEvents)
            {
                recentEvents.Dequeue();
            }
        }
    }

    [Serializable]
    public sealed class BugShotAIReport
    {
        public string timestampUtc;
        public string sceneName;
        public string logType;
        public string condition;
        public string stackTrace;
        public string screenshotPath;
        public float fps;
        public BugShotAIPlayerPosition playerPosition;
        public BugShotAIEvent[] recentEvents;
    }

    [Serializable]
    public sealed class BugShotAIPlayerPosition
    {
        public bool hasPlayer;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class BugShotAIEvent
    {
        public string timestampUtc;
        public float timeSinceStartup;
        public string category;
        public string message;
    }
}
