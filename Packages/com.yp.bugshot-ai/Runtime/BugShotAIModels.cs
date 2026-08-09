using System;

namespace YP.BugShotAI
{
    [Serializable]
    public sealed class BugShotAIReport
    {
        public int schemaVersion = BugShotAIConstants.ReportSchemaVersion;
        public string reportId;
        public string fingerprint;
        public int occurrenceCount;
        public string firstOccurrenceUtc;
        public string timestampUtc;
        public string sceneName;
        public string scenePath;
        public string projectName;
        public string logType;
        public string condition;
        public string stackTrace;
        public string screenshotPath;
        public string screenshotFileName;
        public string relativeScreenshotPath;
        public string markdownFileName;
        public string promptJaFileName;
        public string promptEnFileName;
        public string screenshotError;
        public float fps;
        public bool isPlaying;
        public BugShotAIEditorState editorState;
        public BugShotAIUserNotes userNotes;
        public BugShotAIPlayerPosition playerPosition;
        public BugShotAIEnvironment environment;
        public BugShotAIEvent[] recentEvents;
        public BugShotAILogEntry[] recentLogs;
    }

    [Serializable]
    public sealed class BugShotAIEditorState
    {
        public bool isEditor;
        public bool isPlaying;
        public bool isBatchMode;
        public string platform;
    }

    [Serializable]
    public sealed class BugShotAIUserNotes
    {
        public string reproductionSteps;
        public string expectedResult;
        public string actualResult;
        public string notes;
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
        public string projectName;
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

    [Serializable]
    public sealed class BugShotAILogEntry
    {
        public string timestampUtc;
        public float timeSinceStartup;
        public string logType;
        public string message;
        public string stackTrace;
    }

    public sealed class BugShotAIReportSummary
    {
        public string reportId;
        public string fingerprint;
        public string timestampUtc;
        public string logType;
        public string condition;
        public string sceneName;
        public string reportDirectoryPath;
        public string jsonPath;
        public string markdownPath;
        public string screenshotPath;
        public bool isLegacyReport;
    }

    public sealed class BugShotAISaveResult
    {
        public string reportDirectoryPath;
        public string jsonPath;
        public string markdownPath;
        public string promptJaPath;
        public string promptEnPath;
        public string screenshotPath;
        public string[] deletedReportPaths;
        public string[] cleanupErrors;
    }
}
