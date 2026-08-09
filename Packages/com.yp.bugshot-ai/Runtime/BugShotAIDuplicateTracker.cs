using System;
using System.Collections.Generic;

namespace YP.BugShotAI
{
    public sealed class BugShotAIDuplicateTracker
    {
        private readonly Dictionary<string, BugShotAIDuplicateState> states = new Dictionary<string, BugShotAIDuplicateState>();

        public BugShotAIDuplicateResult Register(string fingerprint, float realtimeSinceStartup, DateTime utcNow, float cooldownSeconds)
        {
            if (string.IsNullOrEmpty(fingerprint))
            {
                fingerprint = "unknown";
            }

            if (!states.TryGetValue(fingerprint, out BugShotAIDuplicateState state))
            {
                state = new BugShotAIDuplicateState
                {
                    firstOccurrenceUtc = utcNow.ToString("o"),
                    lastCapturedRealtime = realtimeSinceStartup,
                    occurrenceCount = 1
                };
                states.Add(fingerprint, state);
                return new BugShotAIDuplicateResult(true, state.occurrenceCount, state.firstOccurrenceUtc);
            }

            state.occurrenceCount++;
            bool shouldCapture = cooldownSeconds <= 0f || realtimeSinceStartup - state.lastCapturedRealtime >= cooldownSeconds;
            if (shouldCapture)
            {
                state.lastCapturedRealtime = realtimeSinceStartup;
            }

            // Suppressed hits still count, but avoid repeating screenshot capture and disk writes during cooldown.
            return new BugShotAIDuplicateResult(shouldCapture, state.occurrenceCount, state.firstOccurrenceUtc);
        }

        private sealed class BugShotAIDuplicateState
        {
            public string firstOccurrenceUtc;
            public float lastCapturedRealtime;
            public int occurrenceCount;
        }
    }

    public readonly struct BugShotAIDuplicateResult
    {
        public BugShotAIDuplicateResult(bool shouldCapture, int occurrenceCount, string firstOccurrenceUtc)
        {
            ShouldCapture = shouldCapture;
            OccurrenceCount = occurrenceCount;
            FirstOccurrenceUtc = firstOccurrenceUtc;
        }

        public bool ShouldCapture { get; }
        public int OccurrenceCount { get; }
        public string FirstOccurrenceUtc { get; }
    }
}
