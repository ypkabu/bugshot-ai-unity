# Changelog

## Unreleased

- Moved settings, privacy masking, fingerprinting, formatting, and report storage out of the Recorder where each has a separate test or failure boundary.
- Added sanitized one-report-per-folder output under `BugShotReports/<report-id>/`.
- Added report IDs, fingerprints, occurrence counts, editor state, user notes, and recent console logs to reports.
- Added project settings support through `ProjectSettings/BugShotAISettings.json` and a `Project > BugShot AI` settings provider.
- Added report history, detail preview, screenshot preview, note editing, Markdown copy, and delete actions to the Editor Window.
- Added storage limits for maximum report count and approximate total report folder size.
- Added EditMode tests for privacy masking, fingerprinting, duplicate suppression, JSON, Markdown, prompt generation, storage policy, filename sanitization, and storage error handling.
- Added a Demo Error Panel sample for NullReferenceException, IndexOutOfRangeException, Debug.LogError, long stack trace, and duplicate burst checks.
- Added a reusable command-line EditMode test runner and Windows PowerShell test script with fixed XML output.
- Added submission validation command-line phases for capture, storage, privacy, duplicate suppression, screenshot fallback, and restart persistence checks.
- Added a Windows Player build smoke command-line utility.
- Extended privacy masking coverage for Linux home paths, UNC paths, Unicode user names, URL fragment secrets, and additional nested output checks.
- Added a concise QA checklist and generated documentation example report workflow.
- Removed outdated MVP screenshots, demo video, and generated demo text assets that no longer matched the current UI and report layout.
- Set the package manifest minimum to Unity 6000.4; verified on Unity 6000.4.6f1 Windows Editor. Unity 2022.3 LTS has not been tested.
- Combined JSON, Markdown, and prompt generation in `BugShotAIReportFormatter`, and moved environment collection into the report builder.
- Reorganized the Editor Window around recording status, report history, and report details using standard Unity IMGUI styles.
- Removed unused output-language and automatic-copy settings.
- Rewrote the public documentation around the original problem, first-version limitations, design decisions, and verified evidence.
- Added report-list timestamps and Stack Trace display to the standard IMGUI detail view.
- Added short reason comments for privacy ordering, fingerprint identity, duplicate cooldown, screenshot fallback, and corrupt report handling.
- Deferred Play Mode screenshots until the frame has finished rendering and serialized concurrent capture requests.
- Bounded the pending screenshot queue and preserves overflow reports with a recorded screenshot omission reason.
- Stopped test-error report polling when it succeeds, times out, the window closes, or another test starts.
- Made the PowerShell validation scripts wait for Unity to exit before evaluating results.
- Added current Editor Window, generated report, privacy preview, and short demo media for the public README.

## 0.1.0

- Added `BugShotAIRecorder` runtime component.
- Added `BugShotAIEventLogger` runtime breadcrumb logger.
- Captures Unity `Error`, `Exception`, and `Assert` logs.
- Saves JSON reports and PNG screenshots under `Application.persistentDataPath/BugShotAI/`.
- Stores scene name, scene path, FPS, optional player position, environment information, screenshot file name, and recent events.
- Added `Tools > BugShot AI > Open Window` Editor Window.
- Added Editor actions for opening reports, creating a recorder, triggering a test error, opening/copying the latest JSON report, and copying/saving GitHub Issue prompts.
- Added Japanese and English GitHub Issue prompt generation.
- Added Basic Setup sample scripts, including a demo floor-clipping bug trigger.
- Added Runtime and Editor asmdef separation.
- Verified on Unity 6000.4.6f1 Windows Editor.
- Unity 2022.3 LTS was not tested for this release.
