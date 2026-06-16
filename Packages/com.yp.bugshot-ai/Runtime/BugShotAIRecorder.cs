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
        private const string PackageVersion = "0.1.0";

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

        public string LastReportPath { get; private set; }

        private void OnValidate()
        {
            maxRecentEvents = Mathf.Max(1, maxRecentEvents);
            duplicateCooldownSeconds = Mathf.Max(0f, duplicateCooldownSeconds);

            if (string.IsNullOrWhiteSpace(outputFolderName))
            {
                outputFolderName = "BugShotAI";
            }
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
                // Warning logs are ignored by ShouldCapture, so this avoids recursive error capture.
                Debug.LogWarning($"[BugShotAI] Failed to capture bug report: {ex}");
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

            string screenshotFileName = $"bugshot_{timestamp}.png";
            string screenshotPath = Path.Combine(outputDirectory, screenshotFileName);
            ScreenCapture.CaptureScreenshot(screenshotPath);
            Scene activeScene = SceneManager.GetActiveScene();

            BugShotAIReport report = new BugShotAIReport
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                sceneName = GetSceneName(activeScene),
                scenePath = NormalizePath(activeScene.path),
                logType = type.ToString(),
                condition = condition,
                stackTrace = stackTrace,
                screenshotPath = NormalizePath(screenshotPath),
                screenshotFileName = screenshotFileName,
                fps = smoothedFps,
                playerPosition = CreatePlayerPosition(),
                environment = CreateEnvironment(),
                recentEvents = recentEvents.ToArray()
            };

            string reportPath = Path.Combine(outputDirectory, $"bugshot_{timestamp}.json");
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(reportPath, json);
            LastReportPath = NormalizePath(reportPath);
        }

        private static string GetSceneName(Scene scene)
        {
            return string.IsNullOrEmpty(scene.name) ? "(Untitled Scene)" : scene.name;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
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

        private BugShotAIEnvironment CreateEnvironment()
        {
            return new BugShotAIEnvironment
            {
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                productName = Application.productName,
                companyName = Application.companyName,
                packageVersion = PackageVersion
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
        public string scenePath;
        public string logType;
        public string condition;
        public string stackTrace;
        public string screenshotPath;
        public string screenshotFileName;
        public float fps;
        public BugShotAIPlayerPosition playerPosition;
        public BugShotAIEnvironment environment;
        public BugShotAIEvent[] recentEvents;
    }

    [Serializable]
    public sealed class BugShotAIEnvironment
    {
        public string unityVersion;
        public string platform;
        public string operatingSystem;
        public string deviceModel;
        public int systemMemorySize;
        public string graphicsDeviceName;
        public string productName;
        public string companyName;
        public string packageVersion;
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
