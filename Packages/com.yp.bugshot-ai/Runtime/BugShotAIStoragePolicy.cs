using System;
using System.Collections.Generic;
using System.Linq;

namespace YP.BugShotAI
{
    public static class BugShotAIStoragePolicy
    {
        public static List<BugShotAIReportFolderInfo> SelectReportsToDelete(
            IEnumerable<BugShotAIReportFolderInfo> reports,
            int maxReportCount,
            long maxStorageBytes)
        {
            List<BugShotAIReportFolderInfo> ordered = reports == null
                ? new List<BugShotAIReportFolderInfo>()
                : reports.OrderBy(report => report.LastWriteTimeUtc).ToList();

            List<BugShotAIReportFolderInfo> deleteTargets = new List<BugShotAIReportFolderInfo>();
            int currentCount = ordered.Count;
            long currentBytes = ordered.Sum(report => Math.Max(0L, report.TotalBytes));

            foreach (BugShotAIReportFolderInfo report in ordered)
            {
                bool countExceeded = maxReportCount > 0 && currentCount > maxReportCount;
                bool storageExceeded = maxStorageBytes > 0 && currentBytes > maxStorageBytes;
                if (!countExceeded && !storageExceeded)
                {
                    break;
                }

                deleteTargets.Add(report);
                currentCount--;
                currentBytes -= Math.Max(0L, report.TotalBytes);
            }

            return deleteTargets;
        }
    }

    public sealed class BugShotAIReportFolderInfo
    {
        public string DirectoryPath;
        public DateTime LastWriteTimeUtc;
        public long TotalBytes;
    }
}
