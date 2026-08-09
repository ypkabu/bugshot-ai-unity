using System;
using System.Collections.Generic;
using UnityEngine;

namespace YP.BugShotAI
{
    public sealed class BugShotAILogRingBuffer
    {
        private readonly Queue<BugShotAILogEntry> logs = new Queue<BugShotAILogEntry>();
        private int maxLogs;

        public BugShotAILogRingBuffer(int maxLogs)
        {
            this.maxLogs = Math.Max(0, maxLogs);
        }

        public void SetMaxLogs(int value)
        {
            maxLogs = Math.Max(0, value);
            Trim();
        }

        public void Record(string message, string stackTrace, LogType type, BugShotAISettings settings)
        {
            if (maxLogs <= 0 || BugShotAITextUtility.StartsWithInternalPrefix(message))
            {
                return;
            }

            logs.Enqueue(new BugShotAILogEntry
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                timeSinceStartup = Time.realtimeSinceStartup,
                logType = type.ToString(),
                message = BugShotAITextUtility.Truncate(message, settings != null ? settings.maxLogChars : 8000),
                stackTrace = BugShotAITextUtility.Truncate(stackTrace, settings != null ? settings.maxStackTraceChars : 12000)
            });

            Trim();
        }

        public BugShotAILogEntry[] Snapshot()
        {
            return logs.ToArray();
        }

        private void Trim()
        {
            while (logs.Count > maxLogs)
            {
                logs.Dequeue();
            }
        }
    }
}
