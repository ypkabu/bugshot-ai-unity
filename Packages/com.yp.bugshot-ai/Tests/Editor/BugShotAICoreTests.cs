using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace YP.BugShotAI.Tests
{
    public sealed class BugShotAICoreTests
    {
        [Test]
        public void PrivacySanitizer_MasksWindowsUserPath()
        {
            string input = @"C:\Users\alice\Project\Assets\Player.cs";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("<USER_HOME>"));
            Assert.That(output, Does.Not.Contain("alice"));
        }

        [Test]
        public void PrivacySanitizer_MasksMacUserPath()
        {
            string input = "/Users/alice/Project/Assets/Player.cs";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("<USER_HOME>"));
            Assert.That(output, Does.Not.Contain("alice"));
        }

        [Test]
        public void PrivacySanitizer_MasksLinuxHomePath()
        {
            string input = "/home/alice/Project/Assets/Player.cs";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("<USER_HOME>"));
            Assert.That(output, Does.Not.Contain("alice"));
        }

        [Test]
        public void PrivacySanitizer_MasksUncPathWithUnicodeUserName()
        {
            string input = @"\\BuildShare\Users\山田\Project\Assets\Player.cs";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("<UNC_PATH>"));
            Assert.That(output, Does.Not.Contain("山田"));
        }

        [Test]
        public void PrivacySanitizer_MasksEmailAddress()
        {
            string output = BugShotAIPrivacySanitizer.Sanitize(
                "contact alice@example.com",
                new BugShotAIPrivacyOptions { maskEmailAddresses = true });

            Assert.That(output, Does.Contain("<EMAIL>"));
            Assert.That(output, Does.Not.Contain("alice@example.com"));
        }

        [Test]
        public void PrivacySanitizer_MasksTokensAndAuthorizationHeader()
        {
            string input = "Authorization: Bearer abc.def.ghi api_key=secret-value github_pat_1234567890abcdef";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("Authorization: <REDACTED>"));
            Assert.That(output, Does.Contain("api_key= <REDACTED>"));
            Assert.That(output, Does.Contain("<GITHUB_TOKEN>"));
            Assert.That(output, Does.Not.Contain("secret-value"));
        }

        [Test]
        public void PrivacySanitizer_MasksAuthorizationHeaderCaseInsensitively()
        {
            string input = "authorization: bearer lower-case-secret";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("authorization: <REDACTED>"));
            Assert.That(output, Does.Not.Contain("lower-case-secret"));
        }

        [Test]
        public void PrivacySanitizer_MasksMultipleSecretsOnSameLine()
        {
            string input = "secret=first-token access_token=second-token client_secret: third-token token=fourth-token";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Not.Contain("first-token"));
            Assert.That(output, Does.Not.Contain("second-token"));
            Assert.That(output, Does.Not.Contain("third-token"));
            Assert.That(output, Does.Not.Contain("fourth-token"));
            Assert.That(output, Does.Contain("<REDACTED>"));
        }

        [Test]
        public void PrivacySanitizer_MasksUrlQuerySecret()
        {
            string input = "https://example.test/report?token=abc123&name=bug";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Contain("token=<REDACTED>"));
            Assert.That(output, Does.Not.Contain("abc123"));
        }

        [Test]
        public void PrivacySanitizer_MasksUrlFragmentSecret()
        {
            string input = "https://example.test/callback#access_token=fragment-secret&state=ok";
            string output = BugShotAIPrivacySanitizer.Sanitize(input, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Not.Contain("fragment-secret"));
            Assert.That(output, Does.Contain("<REDACTED>"));
        }

        [Test]
        public void PrivacySanitizer_MasksIpAddressWhenEnabled()
        {
            string input = "Connected to 192.168.10.24";
            string output = BugShotAIPrivacySanitizer.Sanitize(
                input,
                new BugShotAIPrivacyOptions { maskIpAddresses = true });

            Assert.That(output, Does.Contain("<IP_ADDRESS>"));
            Assert.That(output, Does.Not.Contain("192.168.10.24"));
        }

        [Test]
        public void PrivacySanitizer_HandlesEmptyInput()
        {
            string output = BugShotAIPrivacySanitizer.Sanitize(string.Empty, new BugShotAIPrivacyOptions());

            Assert.That(output, Is.EqualTo(string.Empty));
        }

        [Test]
        public void PrivacySanitizer_HandlesVeryLongInput()
        {
            string secret = "api_key=" + new string('x', 4000);
            string output = BugShotAIPrivacySanitizer.Sanitize(secret, new BugShotAIPrivacyOptions());

            Assert.That(output, Does.Not.Contain(new string('x', 64)));
            Assert.That(output, Does.Contain("<REDACTED>"));
        }

        [Test]
        public void TextUtility_TruncatesLongLog()
        {
            string output = BugShotAITextUtility.Truncate("abcdef", 3);

            Assert.That(output, Does.StartWith("abc"));
            Assert.That(output, Does.Contain("truncated"));
        }

        [Test]
        public void TextUtility_SanitizesInvalidFileName()
        {
            string output = BugShotAITextUtility.SanitizeFileName("bad:name?.json");

            Assert.That(output, Does.Not.Contain(":"));
            Assert.That(output, Does.Not.Contain("?"));
        }

        [Test]
        public void Fingerprint_IsStableForSameError()
        {
            string first = BugShotAIFingerprint.Generate("Error", "boom", "Game.Player:Update()");
            string second = BugShotAIFingerprint.Generate("Error", "boom", "Game.Player:Update()");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Length.EqualTo(16));
        }

        [Test]
        public void Fingerprint_HandlesEmptyStackTrace()
        {
            string fingerprint = BugShotAIFingerprint.Generate("Error", "boom", string.Empty);

            Assert.That(fingerprint, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void DuplicateTracker_SuppressesSameErrorInsideCooldown()
        {
            BugShotAIDuplicateTracker tracker = new BugShotAIDuplicateTracker();

            BugShotAIDuplicateResult first = tracker.Register("abc", 1f, DateTime.UtcNow, 2f);
            BugShotAIDuplicateResult second = tracker.Register("abc", 1.5f, DateTime.UtcNow, 2f);
            BugShotAIDuplicateResult third = tracker.Register("abc", 3.1f, DateTime.UtcNow, 2f);

            Assert.That(first.ShouldCapture, Is.True);
            Assert.That(second.ShouldCapture, Is.False);
            Assert.That(second.OccurrenceCount, Is.EqualTo(2));
            Assert.That(third.ShouldCapture, Is.True);
            Assert.That(third.OccurrenceCount, Is.EqualTo(3));
        }

        [Test]
        public void JsonUtility_GeneratesReadableJson()
        {
            BugShotAIReport report = CreateReport("json-test");
            string json = BugShotAIReportFormatter.ToJson(report);
            BugShotAIReport loaded = BugShotAIReportFormatter.FromJson(json);

            Assert.That(json, Does.Contain("\n"));
            Assert.That(loaded.condition, Is.EqualTo(report.condition));
            Assert.That(loaded.userNotes.actualResult, Is.EqualTo(report.userNotes.actualResult));
        }

        [Test]
        public void JsonUtility_DoesNotIncludeUnsanitizedNestedSecretsAfterSanitize()
        {
            BugShotAIReport report = CreateSensitiveReport("json-sanitized");
            BugShotAIPrivacySanitizer.SanitizeInPlace(report, CreateSensitiveSettings());

            string json = BugShotAIReportFormatter.ToJson(report);

            AssertNoSensitiveData(json);
            Assert.That(json, Does.Contain("<USER_HOME>"));
            Assert.That(json, Does.Contain("<EMAIL>"));
            Assert.That(json, Does.Contain("<IP_ADDRESS>"));
        }

        [Test]
        public void MarkdownGenerator_IncludesCoreSections()
        {
            string markdown = BugShotAIReportFormatter.BuildMarkdown(CreateReport("markdown-test"));

            Assert.That(markdown, Does.Contain("## Summary"));
            Assert.That(markdown, Does.Contain("## Environment"));
            Assert.That(markdown, Does.Contain("## Logs"));
        }

        [Test]
        public void MarkdownGenerator_DoesNotIncludeUnsanitizedAbsolutePathsAfterSanitize()
        {
            BugShotAIReport report = CreateSensitiveReport("markdown-sanitized");
            BugShotAIPrivacySanitizer.SanitizeInPlace(report, CreateSensitiveSettings());

            string markdown = BugShotAIReportFormatter.BuildMarkdown(report);

            AssertNoSensitiveData(markdown);
            Assert.That(markdown, Does.Contain("<USER_HOME>"));
        }

        [Test]
        public void PromptGenerator_BuildsEnglishPrompt()
        {
            BugShotAIReport report = CreateReport("prompt-en");
            string prompt = BugShotAIReportFormatter.BuildEnglishPrompt(report, BugShotAIReportFormatter.ToJson(report));

            Assert.That(prompt, Does.Contain("Markdown GitHub Issue"));
            Assert.That(prompt, Does.Contain("Do not invent unknown information"));
            Assert.That(prompt, Does.Contain("Severity"));
        }

        [Test]
        public void PromptGenerator_DoesNotIncludeUnsanitizedSecretsAfterSanitize()
        {
            BugShotAIReport report = CreateSensitiveReport("prompt-sanitized");
            BugShotAIPrivacySanitizer.SanitizeInPlace(report, CreateSensitiveSettings());
            string json = BugShotAIReportFormatter.ToJson(report);

            string prompt = BugShotAIReportFormatter.BuildEnglishPrompt(report, json);

            AssertNoSensitiveData(prompt);
            Assert.That(prompt, Does.Contain("Use only the sanitized data included below."));
        }

        [Test]
        public void PromptGenerator_BuildsJapanesePrompt()
        {
            BugShotAIReport report = CreateReport("prompt-ja");
            string prompt = BugShotAIReportFormatter.BuildJapanesePrompt(report, BugShotAIReportFormatter.ToJson(report));

            Assert.That(prompt, Does.Contain("GitHub Issue"));
            Assert.That(prompt, Does.Contain("不明な情報は推測せず"));
            Assert.That(prompt, Does.Contain("Severity"));
        }

        [Test]
        public void StoragePolicy_SelectsOldestReportsWhenCountExceeded()
        {
            List<BugShotAIReportFolderInfo> reports = new List<BugShotAIReportFolderInfo>
            {
                new BugShotAIReportFolderInfo { DirectoryPath = "old", LastWriteTimeUtc = new DateTime(2026, 1, 1), TotalBytes = 10 },
                new BugShotAIReportFolderInfo { DirectoryPath = "new", LastWriteTimeUtc = new DateTime(2026, 1, 2), TotalBytes = 10 }
            };

            List<BugShotAIReportFolderInfo> targets = BugShotAIStoragePolicy.SelectReportsToDelete(reports, 1, 1000);

            Assert.That(targets.Select(target => target.DirectoryPath), Is.EqualTo(new[] { "old" }));
        }

        [Test]
        public void StoragePolicy_SelectsOldestReportsWhenStorageExceeded()
        {
            List<BugShotAIReportFolderInfo> reports = new List<BugShotAIReportFolderInfo>
            {
                new BugShotAIReportFolderInfo { DirectoryPath = "old", LastWriteTimeUtc = new DateTime(2026, 1, 1), TotalBytes = 80 },
                new BugShotAIReportFolderInfo { DirectoryPath = "new", LastWriteTimeUtc = new DateTime(2026, 1, 2), TotalBytes = 80 }
            };

            List<BugShotAIReportFolderInfo> targets = BugShotAIStoragePolicy.SelectReportsToDelete(reports, 10, 100);

            Assert.That(targets.Select(target => target.DirectoryPath), Is.EqualTo(new[] { "old" }));
        }

        [Test]
        public void ReportStorage_CreatesFolderAndFilesWhenRootDoesNotExist()
        {
            string root = CreateTempDirectoryPath();
            try
            {
                BugShotAIReport report = CreateReport("storage-test");
                BugShotAISettings settings = BugShotAISettings.CreateDefault();
                BugShotAIReportStorage storage = new BugShotAIReportStorage(root);

                BugShotAISaveResult result = storage.Save(report, new byte[] { 1, 2, 3 }, settings);

                Assert.That(File.Exists(result.jsonPath), Is.True);
                Assert.That(File.Exists(result.markdownPath), Is.True);
                Assert.That(File.Exists(result.promptJaPath), Is.True);
                Assert.That(File.Exists(result.promptEnPath), Is.True);
                Assert.That(File.Exists(result.screenshotPath), Is.True);
            }
            finally
            {
                DeleteDirectoryIfExists(root);
            }
        }

        [Test]
        public void ReportStorage_ThrowsWhenRootIsFile()
        {
            string rootParent = CreateTempDirectoryPath();
            Directory.CreateDirectory(rootParent);
            string filePath = Path.Combine(rootParent, "not-a-directory");
            File.WriteAllText(filePath, "x");

            try
            {
                BugShotAIReportStorage storage = new BugShotAIReportStorage(filePath);

                Assert.Throws<IOException>(() => storage.Save(CreateReport("invalid-root"), null, BugShotAISettings.CreateDefault()));
            }
            finally
            {
                DeleteDirectoryIfExists(rootParent);
            }
        }

        [Test]
        public void PrivacySanitizer_HandlesNullInput()
        {
            Assert.That(BugShotAIPrivacySanitizer.Sanitize(null, new BugShotAIPrivacyOptions()), Is.Null);
        }

        private static BugShotAIReport CreateReport(string reportId)
        {
            return new BugShotAIReport
            {
                schemaVersion = BugShotAIConstants.ReportSchemaVersion,
                reportId = reportId,
                fingerprint = "abcdef1234567890",
                occurrenceCount = 1,
                firstOccurrenceUtc = "2026-07-31T00:00:00Z",
                timestampUtc = "2026-07-31T00:00:00Z",
                sceneName = "DemoScene",
                scenePath = "Assets/Scenes/DemoScene.unity",
                projectName = "BugShotAI Demo",
                logType = "Error",
                condition = "Player fell through the floor near the right platform.",
                stackTrace = "BugShotAIDemoBugTrigger:TriggerDemoBug ()",
                screenshotFileName = "screenshot.png",
                relativeScreenshotPath = reportId + "/screenshot.png",
                fps = 60f,
                isPlaying = true,
                editorState = new BugShotAIEditorState
                {
                    isEditor = true,
                    isPlaying = true,
                    isBatchMode = false,
                    platform = "WindowsEditor"
                },
                userNotes = new BugShotAIUserNotes
                {
                    reproductionSteps = "Press D, LeftShift, Space, then B.",
                    expectedResult = "The player stays on the platform.",
                    actualResult = "The player fell through the floor.",
                    notes = "Demo report."
                },
                environment = new BugShotAIEnvironment
                {
                    unityVersion = "6000.4.6f1",
                    platform = "WindowsEditor",
                    operatingSystem = "Windows",
                    graphicsDeviceName = "GPU",
                    projectName = "BugShotAI Demo",
                    productName = "BugShotAI Demo",
                    companyName = "YP",
                    packageVersion = BugShotAIConstants.PackageVersion
                },
                playerPosition = new BugShotAIPlayerPosition
                {
                    hasPlayer = true,
                    x = 1f,
                    y = -3f,
                    z = 0f
                },
                recentEvents = new[]
                {
                    new BugShotAIEvent
                    {
                        category = "Player",
                        message = "Pressed jump"
                    }
                },
                recentLogs = new[]
                {
                    new BugShotAILogEntry
                    {
                        logType = "Error",
                        message = "Player fell through the floor near the right platform.",
                        stackTrace = "BugShotAIDemoBugTrigger:TriggerDemoBug ()"
                    }
                }
            };
        }

        private static BugShotAIReport CreateSensitiveReport(string reportId)
        {
            BugShotAIReport report = CreateReport(reportId);
            report.scenePath = @"C:\Users\alice\SecretProject\Assets\Scenes\DemoScene.unity";
            report.condition = "Failed to upload report for alice@example.com with token=condition-secret";
            report.stackTrace =
                "Demo:Run () (at C:/Users/alice/SecretProject/Assets/Demo.cs:10)\n" +
                "Mac:Run () (at /Users/bob/SecretProject/Assets/Mac.cs:20)\n" +
                "Linux:Run () (at /home/charlie/SecretProject/Assets/Linux.cs:30)\n" +
                "UNC:Run () (at \\\\BuildShare\\Users\\山田\\SecretProject\\Assets\\Unc.cs:40)\n" +
                "Authorization: Bearer stack-secret api_key=stack-api-key\n" +
                "https://example.test/callback#access_token=fragment-secret\n" +
                "Connected to 192.168.10.24";
            report.userNotes.notes = "Contact alice@example.com with client_secret: nested-secret";
            report.environment.operatingSystem = "Windows user path C:/Users/alice/AppData";
            report.recentEvents = new[]
            {
                new BugShotAIEvent
                {
                    category = "Secret",
                    message = "Loaded /home/charlie/config.json secret=event-secret"
                }
            };
            report.recentLogs = new[]
            {
                new BugShotAILogEntry
                {
                    logType = "Log",
                    message = "Saved to C:/Users/alice/file.txt access_token=log-secret",
                    stackTrace = @"\\BuildShare\Users\山田\file.cs"
                }
            };

            return report;
        }

        private static BugShotAISettings CreateSensitiveSettings()
        {
            BugShotAISettings settings = BugShotAISettings.CreateDefault();
            settings.maskIpAddresses = true;
            return settings;
        }

        private static void AssertNoSensitiveData(string output)
        {
            Assert.That(output, Does.Not.Contain("alice"));
            Assert.That(output, Does.Not.Contain("bob"));
            Assert.That(output, Does.Not.Contain("charlie"));
            Assert.That(output, Does.Not.Contain("山田"));
            Assert.That(output, Does.Not.Contain("condition-secret"));
            Assert.That(output, Does.Not.Contain("stack-secret"));
            Assert.That(output, Does.Not.Contain("stack-api-key"));
            Assert.That(output, Does.Not.Contain("fragment-secret"));
            Assert.That(output, Does.Not.Contain("nested-secret"));
            Assert.That(output, Does.Not.Contain("event-secret"));
            Assert.That(output, Does.Not.Contain("log-secret"));
            Assert.That(output, Does.Not.Contain("192.168.10.24"));
            Assert.That(output, Does.Not.Contain("alice@example.com"));
        }

        private static string CreateTempDirectoryPath()
        {
            return Path.Combine(Path.GetTempPath(), "BugShotAITests", Guid.NewGuid().ToString("N"));
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
