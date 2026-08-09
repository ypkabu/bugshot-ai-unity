using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YP.BugShotAI
{
    public sealed class BugShotAIReportStorage
    {
        private const string JsonFileName = "report.json";
        private const string MarkdownFileName = "report.md";
        private const string ScreenshotFileName = "screenshot.png";
        private const string PromptJaFileName = "prompt_ja.txt";
        private const string PromptEnFileName = "prompt_en.txt";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string rootPath;

        public BugShotAIReportStorage(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public BugShotAISaveResult Save(BugShotAIReport report, byte[] screenshotPngBytes, BugShotAISettings settings)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (settings == null)
            {
                settings = BugShotAISettings.CreateDefault();
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                throw new InvalidOperationException("Report root path is empty.");
            }

            Directory.CreateDirectory(rootPath);

            string reportDirectoryName = BugShotAITextUtility.SanitizeFileName(report.reportId);
            string reportDirectoryPath = Path.Combine(rootPath, reportDirectoryName);
            Directory.CreateDirectory(reportDirectoryPath);

            string jsonPath = Path.Combine(reportDirectoryPath, JsonFileName);
            string markdownPath = Path.Combine(reportDirectoryPath, MarkdownFileName);
            string promptJaPath = Path.Combine(reportDirectoryPath, PromptJaFileName);
            string promptEnPath = Path.Combine(reportDirectoryPath, PromptEnFileName);
            string screenshotPath = screenshotPngBytes != null && screenshotPngBytes.Length > 0
                ? Path.Combine(reportDirectoryPath, ScreenshotFileName)
                : null;

            report.screenshotFileName = screenshotPngBytes != null && screenshotPngBytes.Length > 0 ? ScreenshotFileName : null;
            report.relativeScreenshotPath = report.screenshotFileName == null
                ? null
                : BugShotAITextUtility.NormalizePath(Path.Combine(reportDirectoryName, ScreenshotFileName));
            report.screenshotPath = report.relativeScreenshotPath;
            report.markdownFileName = MarkdownFileName;
            report.promptJaFileName = PromptJaFileName;
            report.promptEnFileName = PromptEnFileName;

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                try
                {
                    File.WriteAllBytes(screenshotPath, screenshotPngBytes);
                }
                catch (Exception ex)
                {
                    report.screenshotFileName = null;
                    report.relativeScreenshotPath = null;
                    report.screenshotPath = null;
                    report.screenshotError = BugShotAITextUtility.NullIfEmpty(ex.Message);
                    screenshotPath = null;
                }
            }

            string json = BugShotAIReportFormatter.ToJson(report);
            string markdown = BugShotAIReportFormatter.BuildMarkdown(report);
            string promptJa = BugShotAIReportFormatter.BuildJapanesePrompt(report, json);
            string promptEn = BugShotAIReportFormatter.BuildEnglishPrompt(report, json);

            File.WriteAllText(jsonPath, json, Utf8NoBom);
            File.WriteAllText(markdownPath, markdown, Utf8NoBom);
            File.WriteAllText(promptJaPath, promptJa, Utf8NoBom);
            File.WriteAllText(promptEnPath, promptEn, Utf8NoBom);

            BugShotAICleanupResult cleanupResult = Cleanup(settings);

            return new BugShotAISaveResult
            {
                reportDirectoryPath = BugShotAITextUtility.NormalizePath(reportDirectoryPath),
                jsonPath = BugShotAITextUtility.NormalizePath(jsonPath),
                markdownPath = BugShotAITextUtility.NormalizePath(markdownPath),
                promptJaPath = BugShotAITextUtility.NormalizePath(promptJaPath),
                promptEnPath = BugShotAITextUtility.NormalizePath(promptEnPath),
                screenshotPath = BugShotAITextUtility.NormalizePath(screenshotPath),
                deletedReportPaths = cleanupResult.DeletedPaths,
                cleanupErrors = cleanupResult.Errors
            };
        }

        public List<BugShotAIReportSummary> FindReports(bool includeLegacyFlatReports)
        {
            List<BugShotAIReportSummary> reports = new List<BugShotAIReportSummary>();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                return reports;
            }

            foreach (string directory in Directory.EnumerateDirectories(rootPath))
            {
                string jsonPath = Path.Combine(directory, JsonFileName);
                if (!File.Exists(jsonPath))
                {
                    continue;
                }

                BugShotAIReportSummary summary = TryReadSummary(jsonPath, directory, false);
                if (summary != null)
                {
                    reports.Add(summary);
                }
            }

            if (includeLegacyFlatReports)
            {
                foreach (string jsonPath in Directory.EnumerateFiles(rootPath, "bugshot_*.json"))
                {
                    BugShotAIReportSummary summary = TryReadSummary(jsonPath, rootPath, true);
                    if (summary != null)
                    {
                        reports.Add(summary);
                    }
                }
            }

            return reports
                .OrderByDescending(report => File.Exists(report.jsonPath) ? File.GetLastWriteTimeUtc(report.jsonPath) : DateTime.MinValue)
                .ToList();
        }

        public BugShotAIReport LoadReport(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                return null;
            }

            return BugShotAIReportFormatter.FromJson(File.ReadAllText(jsonPath));
        }

        public void SaveExistingReport(BugShotAIReport report, string jsonPath, BugShotAISettings settings)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (string.IsNullOrEmpty(jsonPath))
            {
                throw new ArgumentException("JSON path is empty.", nameof(jsonPath));
            }

            BugShotAIPrivacySanitizer.SanitizeInPlace(report, settings);

            string directory = Path.GetDirectoryName(jsonPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Report directory is unavailable.");
            }

            string json = BugShotAIReportFormatter.ToJson(report);
            File.WriteAllText(jsonPath, json, Utf8NoBom);

            string markdownPath = Path.Combine(directory, MarkdownFileName);
            string promptJaPath = Path.Combine(directory, PromptJaFileName);
            string promptEnPath = Path.Combine(directory, PromptEnFileName);
            File.WriteAllText(markdownPath, BugShotAIReportFormatter.BuildMarkdown(report), Utf8NoBom);
            File.WriteAllText(promptJaPath, BugShotAIReportFormatter.BuildJapanesePrompt(report, json), Utf8NoBom);
            File.WriteAllText(promptEnPath, BugShotAIReportFormatter.BuildEnglishPrompt(report, json), Utf8NoBom);
        }

        public bool DeleteReport(BugShotAIReportSummary summary, out string error)
        {
            error = null;
            if (summary == null)
            {
                error = "Report summary is null.";
                return false;
            }

            try
            {
                if (summary.isLegacyReport)
                {
                    DeleteFileIfExists(summary.jsonPath);
                    DeleteFileIfExists(summary.markdownPath);
                    DeleteFileIfExists(summary.screenshotPath);
                }
                else if (!string.IsNullOrEmpty(summary.reportDirectoryPath) && Directory.Exists(summary.reportDirectoryPath))
                {
                    Directory.Delete(summary.reportDirectoryPath, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private BugShotAICleanupResult Cleanup(BugShotAISettings settings)
        {
            List<string> deleted = new List<string>();
            List<string> errors = new List<string>();
            List<BugShotAIReportFolderInfo> reportFolders = GetReportFolderInfos();
            long maxBytes = (long)Math.Max(1, settings.maxStorageMegabytes) * 1024L * 1024L;
            List<BugShotAIReportFolderInfo> targets = BugShotAIStoragePolicy.SelectReportsToDelete(
                reportFolders,
                settings.maxReportCount,
                maxBytes);

            foreach (BugShotAIReportFolderInfo target in targets)
            {
                try
                {
                    Directory.Delete(target.DirectoryPath, true);
                    deleted.Add(BugShotAITextUtility.NormalizePath(target.DirectoryPath));
                }
                catch (Exception ex)
                {
                    errors.Add($"{BugShotAITextUtility.NormalizePath(target.DirectoryPath)}: {ex.Message}");
                }
            }

            return new BugShotAICleanupResult(deleted.ToArray(), errors.ToArray());
        }

        private List<BugShotAIReportFolderInfo> GetReportFolderInfos()
        {
            List<BugShotAIReportFolderInfo> folders = new List<BugShotAIReportFolderInfo>();
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                return folders;
            }

            foreach (string directory in Directory.EnumerateDirectories(rootPath))
            {
                string jsonPath = Path.Combine(directory, JsonFileName);
                if (!File.Exists(jsonPath))
                {
                    continue;
                }

                folders.Add(new BugShotAIReportFolderInfo
                {
                    DirectoryPath = directory,
                    LastWriteTimeUtc = Directory.GetLastWriteTimeUtc(directory),
                    TotalBytes = CalculateDirectorySize(directory)
                });
            }

            return folders;
        }

        private static BugShotAIReportSummary TryReadSummary(string jsonPath, string reportDirectoryPath, bool legacy)
        {
            try
            {
                BugShotAIReport report = BugShotAIReportFormatter.FromJson(File.ReadAllText(jsonPath));
                if (report == null)
                {
                    return null;
                }

                string screenshotPath = ResolveScreenshotPath(reportDirectoryPath, report);
                string markdownPath = legacy ? null : Path.Combine(reportDirectoryPath, MarkdownFileName);

                return new BugShotAIReportSummary
                {
                    reportId = report.reportId,
                    fingerprint = report.fingerprint,
                    timestampUtc = report.timestampUtc,
                    logType = report.logType,
                    condition = report.condition,
                    sceneName = report.sceneName,
                    reportDirectoryPath = BugShotAITextUtility.NormalizePath(reportDirectoryPath),
                    jsonPath = BugShotAITextUtility.NormalizePath(jsonPath),
                    markdownPath = BugShotAITextUtility.NormalizePath(markdownPath),
                    screenshotPath = BugShotAITextUtility.NormalizePath(screenshotPath),
                    isLegacyReport = legacy
                };
            }
            catch
            {
                // One damaged report folder should not prevent the rest of the history from opening.
                return null;
            }
        }

        private static string ResolveScreenshotPath(string reportDirectoryPath, BugShotAIReport report)
        {
            if (report == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(report.screenshotFileName))
            {
                string directPath = Path.Combine(reportDirectoryPath, report.screenshotFileName);
                if (File.Exists(directPath))
                {
                    return directPath;
                }
            }

            if (!string.IsNullOrEmpty(report.screenshotPath) && File.Exists(report.screenshotPath))
            {
                return report.screenshotPath;
            }

            return null;
        }

        private static long CalculateDirectorySize(string directory)
        {
            long total = 0L;
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Ignore files that disappear during cleanup calculation.
                }
            }

            return total;
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private readonly struct BugShotAICleanupResult
        {
            public BugShotAICleanupResult(string[] deletedPaths, string[] errors)
            {
                DeletedPaths = deletedPaths;
                Errors = errors;
            }

            public string[] DeletedPaths { get; }
            public string[] Errors { get; }
        }
    }
}
