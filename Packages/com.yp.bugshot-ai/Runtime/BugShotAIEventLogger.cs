using System;

namespace YP.BugShotAI
{
    /// <summary>Records a gameplay breadcrumb for the next report.</summary>
    public static class BugShotAIEventLogger
    {
        public static event Action<string, string> EventRecorded;

        public static void Record(string category, string message)
        {
            EventRecorded?.Invoke(category, message);
        }
    }
}
