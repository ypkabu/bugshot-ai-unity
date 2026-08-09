using System.Text;
using UnityEngine;

namespace YP.BugShotAI
{
    public enum BugShotAIPromptLanguage
    {
        English,
        Japanese
    }

    public static class BugShotAIReportFormatter
    {
        public static string ToJson(BugShotAIReport report)
        {
            return JsonUtility.ToJson(report, true);
        }

        public static BugShotAIReport FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<BugShotAIReport>(json);
        }

        public static string BuildMarkdown(BugShotAIReport report)
        {
            StringBuilder builder = new StringBuilder();
            string title = string.IsNullOrWhiteSpace(report?.condition)
                ? "BugShot AI Report"
                : report.condition;

            builder.AppendLine("# " + title);
            builder.AppendLine();
            AppendSummary(builder, report);
            AppendEnvironment(builder, report);
            AppendUserNotes(builder, report);
            AppendLogs(builder, report);
            AppendScreenshot(builder, report);
            return builder.ToString();
        }

        public static string BuildPrompt(BugShotAIReport report, string json, BugShotAIPromptLanguage language)
        {
            return language == BugShotAIPromptLanguage.Japanese
                ? BuildJapanesePrompt(report, json)
                : BuildEnglishPrompt(report, json);
        }

        public static string BuildEnglishPrompt(BugShotAIReport report, string json)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Create a GitHub Issue from the following BugShot AI report.");
            builder.AppendLine();
            builder.AppendLine("Output rules:");
            builder.AppendLine("- Output a Markdown GitHub Issue.");
            builder.AppendLine("- Include Title, Summary, Environment, Steps to Reproduce, Expected Result, Actual Result, Logs, Screenshot, and Severity.");
            builder.AppendLine("- Do not invent unknown information.");
            builder.AppendLine("- Use `Unknown` when information is missing.");
            builder.AppendLine("- Use recentEvents and userNotes to infer reproduction steps when possible.");
            builder.AppendLine("- Extract important lines from stackTrace.");
            builder.AppendLine("- Choose Severity from Critical / High / Medium / Low.");
            builder.AppendLine("- Use only the sanitized data included below.");
            builder.AppendLine();
            builder.AppendLine("JSON Report File: report.json");
            builder.AppendLine("Screenshot File: " + BugShotAITextUtility.UnknownIfEmpty(report?.screenshotFileName));
            builder.AppendLine();
            AppendJson(builder, json);
            return builder.ToString();
        }

        public static string BuildJapanesePrompt(BugShotAIReport report, string json)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("以下のBugShot AIレポートをもとに、GitHub Issueを生成してください。");
            builder.AppendLine();
            builder.AppendLine("出力ルール:");
            builder.AppendLine("- Markdown形式で出力してください。");
            builder.AppendLine("- Title / Summary / Environment / Steps to Reproduce / Expected Result / Actual Result / Logs / Screenshot / Severity を含めてください。");
            builder.AppendLine("- Severity は Critical / High / Medium / Low から1つ選んでください。");
            builder.AppendLine("- 不明な情報は推測せず Unknown と書いてください。");
            builder.AppendLine("- recentEvents と userNotes がある場合は再現手順の推定に使ってください。");
            builder.AppendLine("- stackTrace から重要そうな行を抜き出してください。");
            builder.AppendLine("- 下に含まれるマスク済みデータだけを使ってください。");
            builder.AppendLine();
            builder.AppendLine("JSON Report File: report.json");
            builder.AppendLine("Screenshot File: " + BugShotAITextUtility.UnknownIfEmpty(report?.screenshotFileName));
            builder.AppendLine();
            AppendJson(builder, json);
            return builder.ToString();
        }

        private static void AppendSummary(StringBuilder builder, BugShotAIReport report)
        {
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine("- Report ID: " + Unknown(report?.reportId));
            builder.AppendLine("- Timestamp UTC: " + Unknown(report?.timestampUtc));
            builder.AppendLine("- Log Type: " + Unknown(report?.logType));
            builder.AppendLine("- Scene: " + Unknown(report?.sceneName));
            builder.AppendLine("- Scene Path: " + Unknown(report?.scenePath));
            builder.AppendLine("- Fingerprint: " + Unknown(report?.fingerprint));
            builder.AppendLine("- Occurrence Count: " + (report != null ? report.occurrenceCount.ToString() : "Unknown"));
            builder.AppendLine();
        }

        private static void AppendEnvironment(StringBuilder builder, BugShotAIReport report)
        {
            BugShotAIEnvironment environment = report?.environment;
            builder.AppendLine("## Environment");
            builder.AppendLine();
            builder.AppendLine("- Unity Version: " + Unknown(environment?.unityVersion));
            builder.AppendLine("- Platform: " + Unknown(environment?.platform));
            builder.AppendLine("- OS: " + Unknown(environment?.operatingSystem));
            builder.AppendLine("- Graphics: " + Unknown(environment?.graphicsDeviceName));
            builder.AppendLine("- Product Name: " + Unknown(environment?.productName));
            builder.AppendLine("- Company Name: " + Unknown(environment?.companyName));
            builder.AppendLine("- BugShot Version: " + Unknown(environment?.packageVersion));
            builder.AppendLine();
        }

        private static void AppendUserNotes(StringBuilder builder, BugShotAIReport report)
        {
            BugShotAIUserNotes notes = report?.userNotes;
            builder.AppendLine("## Steps to Reproduce");
            builder.AppendLine();
            builder.AppendLine(Unknown(notes?.reproductionSteps));
            builder.AppendLine();
            builder.AppendLine("## Expected Result");
            builder.AppendLine();
            builder.AppendLine(Unknown(notes?.expectedResult));
            builder.AppendLine();
            builder.AppendLine("## Actual Result");
            builder.AppendLine();
            builder.AppendLine(Unknown(notes?.actualResult ?? report?.condition));
            builder.AppendLine();
            builder.AppendLine("## Notes");
            builder.AppendLine();
            builder.AppendLine(Unknown(notes?.notes));
            builder.AppendLine();
        }

        private static void AppendLogs(StringBuilder builder, BugShotAIReport report)
        {
            builder.AppendLine("## Logs");
            builder.AppendLine();
            builder.AppendLine("- Condition: `" + Unknown(report?.condition) + "`");
            builder.AppendLine("- Stack Trace:");
            builder.AppendLine("```text");
            builder.AppendLine(Unknown(report?.stackTrace));
            builder.AppendLine("```");

            if (report?.recentEvents != null && report.recentEvents.Length > 0)
            {
                builder.AppendLine("- Recent Events:");
                for (int i = 0; i < report.recentEvents.Length; i++)
                {
                    BugShotAIEvent evt = report.recentEvents[i];
                    builder.AppendLine($"  - [{Unknown(evt?.category)}] {Unknown(evt?.message)}");
                }
            }

            if (report?.recentLogs != null && report.recentLogs.Length > 0)
            {
                builder.AppendLine("- Recent Console Logs:");
                for (int i = 0; i < report.recentLogs.Length; i++)
                {
                    BugShotAILogEntry log = report.recentLogs[i];
                    builder.AppendLine($"  - [{Unknown(log?.logType)}] {Unknown(log?.message)}");
                }
            }

            builder.AppendLine();
        }

        private static void AppendScreenshot(StringBuilder builder, BugShotAIReport report)
        {
            builder.AppendLine("## Screenshot");
            builder.AppendLine();
            builder.AppendLine(Unknown(report?.relativeScreenshotPath ?? report?.screenshotFileName));
            if (!string.IsNullOrWhiteSpace(report?.screenshotError))
            {
                builder.AppendLine();
                builder.AppendLine("Screenshot capture error: " + report.screenshotError);
            }
        }

        private static void AppendJson(StringBuilder builder, string json)
        {
            builder.AppendLine("JSON:");
            builder.AppendLine("```json");
            builder.AppendLine(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            builder.AppendLine("```");
        }

        private static string Unknown(string value)
        {
            return BugShotAITextUtility.UnknownIfEmpty(value);
        }
    }
}
