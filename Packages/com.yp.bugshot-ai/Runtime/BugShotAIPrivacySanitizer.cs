using System;
using System.Text.RegularExpressions;

namespace YP.BugShotAI
{
    public sealed class BugShotAIPrivacyOptions
    {
        public bool maskEmailAddresses = true;
        public bool maskIpAddresses;
        public string userHomePath;
        public string projectRootPath;

        public static BugShotAIPrivacyOptions FromSettings(BugShotAISettings settings)
        {
            return new BugShotAIPrivacyOptions
            {
                maskEmailAddresses = settings == null || settings.maskEmailAddresses,
                maskIpAddresses = settings != null && settings.maskIpAddresses,
                userHomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                projectRootPath = BugShotAIPathUtility.GetProjectRootPath()
            };
        }
    }

    public static class BugShotAIPrivacySanitizer
    {
        private static readonly Regex WindowsUserPathRegex = new Regex(@"\b[A-Z]:[\\/]+Users[\\/]+[^\\/""'\s]+", RegexOptions.IgnoreCase);
        private static readonly Regex MacUserPathRegex = new Regex(@"(?<!\w)/Users/[^/""'\s]+", RegexOptions.IgnoreCase);
        private static readonly Regex LinuxHomePathRegex = new Regex(@"(?<!\w)/home/[^/""'\s]+", RegexOptions.IgnoreCase);
        private static readonly Regex UncUserPathRegex = new Regex(@"\\\\[^\\/""'\s]+[\\/]+(?:Users[\\/]+)?[^\\/""'\s]+", RegexOptions.IgnoreCase);
        private static readonly Regex EmailRegex = new Regex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase);
        private static readonly Regex AuthorizationHeaderRegex = new Regex(@"(?i)(Authorization\s*:\s*)(?:Bearer\s+)?[A-Za-z0-9._~+/=\-]+");
        private static readonly Regex BearerTokenRegex = new Regex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=\-]+");
        private static readonly Regex GitHubTokenRegex = new Regex(@"(?i)\b(?:ghp|gho|ghu|ghs|ghr|github_pat)_[A-Za-z0-9_]+\b");
        private static readonly Regex SecretAssignmentRegex = new Regex(@"(?i)(?<![?&])\b(api[-_ ]?key|access[-_ ]?token|github[-_ ]?token|client[-_ ]?secret|secret|token)\b\s*[:=]\s*[""']?[^""'\s,;&]+");
        private static readonly Regex UrlSecretRegex = new Regex(@"(?i)([?#&](?:token|key|secret|api_key|access_token|client_secret)=)[^&#\s]+");
        private static readonly Regex IpAddressRegex = new Regex(@"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\b");

        public static string Sanitize(string value, BugShotAIPrivacyOptions options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (options == null)
            {
                options = new BugShotAIPrivacyOptions();
            }

            string sanitized = value;
            // Specific roots go first so project paths keep the more useful <PROJECT_ROOT> label.
            sanitized = ReplaceKnownPath(sanitized, options.projectRootPath, "<PROJECT_ROOT>");
            sanitized = ReplaceKnownPath(sanitized, options.userHomePath, "<USER_HOME>");
            sanitized = WindowsUserPathRegex.Replace(sanitized, "<USER_HOME>");
            sanitized = MacUserPathRegex.Replace(sanitized, "<USER_HOME>");
            sanitized = LinuxHomePathRegex.Replace(sanitized, "<USER_HOME>");
            sanitized = UncUserPathRegex.Replace(sanitized, "<UNC_PATH>");

            if (options.maskEmailAddresses)
            {
                sanitized = EmailRegex.Replace(sanitized, "<EMAIL>");
            }

            sanitized = AuthorizationHeaderRegex.Replace(sanitized, "$1<REDACTED>");
            sanitized = BearerTokenRegex.Replace(sanitized, "Bearer <REDACTED>");
            sanitized = GitHubTokenRegex.Replace(sanitized, "<GITHUB_TOKEN>");
            sanitized = SecretAssignmentRegex.Replace(sanitized, MaskSecretAssignment);
            sanitized = UrlSecretRegex.Replace(sanitized, "$1<REDACTED>");

            if (options.maskIpAddresses)
            {
                sanitized = IpAddressRegex.Replace(sanitized, "<IP_ADDRESS>");
            }

            return sanitized;
        }

        public static void SanitizeInPlace(BugShotAIReport report, BugShotAISettings settings)
        {
            if (report == null)
            {
                return;
            }

            BugShotAIPrivacyOptions options = BugShotAIPrivacyOptions.FromSettings(settings);
            report.reportId = Sanitize(report.reportId, options);
            report.fingerprint = Sanitize(report.fingerprint, options);
            report.sceneName = Sanitize(report.sceneName, options);
            report.scenePath = Sanitize(report.scenePath, options);
            report.projectName = Sanitize(report.projectName, options);
            report.condition = Sanitize(report.condition, options);
            report.stackTrace = Sanitize(report.stackTrace, options);
            report.screenshotPath = Sanitize(report.screenshotPath, options);
            report.screenshotFileName = Sanitize(report.screenshotFileName, options);
            report.relativeScreenshotPath = Sanitize(report.relativeScreenshotPath, options);
            report.screenshotError = Sanitize(report.screenshotError, options);

            if (report.userNotes != null)
            {
                report.userNotes.reproductionSteps = Sanitize(report.userNotes.reproductionSteps, options);
                report.userNotes.expectedResult = Sanitize(report.userNotes.expectedResult, options);
                report.userNotes.actualResult = Sanitize(report.userNotes.actualResult, options);
                report.userNotes.notes = Sanitize(report.userNotes.notes, options);
            }

            if (report.environment != null)
            {
                report.environment.unityVersion = Sanitize(report.environment.unityVersion, options);
                report.environment.platform = Sanitize(report.environment.platform, options);
                report.environment.operatingSystem = Sanitize(report.environment.operatingSystem, options);
                report.environment.deviceModel = Sanitize(report.environment.deviceModel, options);
                report.environment.graphicsDeviceName = Sanitize(report.environment.graphicsDeviceName, options);
                report.environment.projectName = Sanitize(report.environment.projectName, options);
                report.environment.productName = Sanitize(report.environment.productName, options);
                report.environment.companyName = Sanitize(report.environment.companyName, options);
                report.environment.packageVersion = Sanitize(report.environment.packageVersion, options);
            }

            SanitizeEvents(report.recentEvents, options);
            SanitizeLogs(report.recentLogs, options);
        }

        private static void SanitizeEvents(BugShotAIEvent[] events, BugShotAIPrivacyOptions options)
        {
            if (events == null)
            {
                return;
            }

            for (int i = 0; i < events.Length; i++)
            {
                if (events[i] == null)
                {
                    continue;
                }

                events[i].category = Sanitize(events[i].category, options);
                events[i].message = Sanitize(events[i].message, options);
            }
        }

        private static void SanitizeLogs(BugShotAILogEntry[] logs, BugShotAIPrivacyOptions options)
        {
            if (logs == null)
            {
                return;
            }

            for (int i = 0; i < logs.Length; i++)
            {
                if (logs[i] == null)
                {
                    continue;
                }

                logs[i].logType = Sanitize(logs[i].logType, options);
                logs[i].message = Sanitize(logs[i].message, options);
                logs[i].stackTrace = Sanitize(logs[i].stackTrace, options);
            }
        }

        private static string ReplaceKnownPath(string value, string path, string token)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(path))
            {
                return value;
            }

            string normalizedPath = BugShotAITextUtility.NormalizePath(path).TrimEnd('/');
            if (string.IsNullOrEmpty(normalizedPath))
            {
                return value;
            }

            string pattern = Regex.Escape(normalizedPath).Replace("/", "[/\\\\]");
            return Regex.Replace(value, pattern, token, RegexOptions.IgnoreCase);
        }

        private static string MaskSecretAssignment(Match match)
        {
            int separatorIndex = match.Value.IndexOf(':');
            if (separatorIndex < 0)
            {
                separatorIndex = match.Value.IndexOf('=');
            }

            if (separatorIndex < 0)
            {
                return "<REDACTED>";
            }

            return match.Value.Substring(0, separatorIndex + 1) + " <REDACTED>";
        }
    }
}
