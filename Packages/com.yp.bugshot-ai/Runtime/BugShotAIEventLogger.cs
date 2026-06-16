using System;

namespace YP.BugShotAI
{
    /// <summary>
    /// Use this static helper from gameplay code to add simple breadcrumbs before an error happens.
    /// Example: BugShotAIEventLogger.Record("Player", "Picked up key");
    /// </summary>
    public static class BugShotAIEventLogger
    {
        public static event Action<string, string> EventRecorded;

        public static void Record(string category, string message)
        {
            EventRecorded?.Invoke(category, message);
        }
    }
}
