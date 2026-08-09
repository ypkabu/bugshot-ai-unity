# Testing

BugShot AI test and validation utilities live under:

```text
Packages/com.yp.bugshot-ai/Tests/Editor/
Packages/com.yp.bugshot-ai/Tools~/
```

## EditMode Tests

Run from a Unity project that contains or references the package:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunEditModeTests.ps1
```

Optional arguments:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunEditModeTests.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

Outputs:

```text
TestResults/BugShotAI_EditMode.xml
Logs/BugShotAI_EditMode.log
```

The script executes:

```text
YP.BugShotAI.Tests.BugShotAICommandLineTestRunner.RunEditModeTests
```

## Submission Validation

Run:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunSubmissionValidation.ps1
```

Optional arguments:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunSubmissionValidation.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

Outputs:

```text
Logs/BugShotAI_SubmissionValidation_RunAll.json
Logs/BugShotAI_SubmissionValidation_RunAll.md
Logs/BugShotAI_SubmissionValidation_PersistencePhase1.json
Logs/BugShotAI_SubmissionValidation_PersistencePhase2.json
```

The script runs:

```text
YP.BugShotAI.Tests.BugShotAISubmissionValidation.RunAll
YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase1
YP.BugShotAI.Tests.BugShotAISubmissionValidation.PersistencePhase2
```

It exits non-zero if Unity exits non-zero, if result files are missing, or if a result JSON contains `failedCount > 0`.

## Windows Player Build Smoke

Run:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunWindowsPlayerBuildSmoke.ps1
```

Optional arguments:

```powershell
powershell -ExecutionPolicy Bypass -File Packages/com.yp.bugshot-ai/Tools~/RunWindowsPlayerBuildSmoke.ps1 -UnityPath "<Unity.exe>" -ProjectPath "<project>"
```

Outputs:

```text
Builds/BugShotAIPlayerSmoke/<timestamp>/BugShotAIPlayerSmoke.exe
Logs/BugShotAI_player_build_smoke.log
```

If no enabled build scene exists, the smoke test creates a temporary scene asset for the build and deletes it afterward.

## Covered By EditMode Tests

- Windows user path masking
- macOS user path masking
- Linux home path masking
- UNC path masking
- Unicode user name masking
- Email masking
- Authorization, Bearer, GitHub token, and API key-like masking
- Case-insensitive Authorization masking
- Multiple secrets on the same line
- URL query secret masking
- URL fragment secret masking
- Optional IP address masking
- Empty, null, and very long input handling
- StackTrace masking
- Nested report string masking before JSON output
- Markdown masking
- Prompt masking
- Long log truncation
- Filename sanitization
- Fingerprint stability
- Empty stack trace handling
- Duplicate suppression and occurrence counting
- JSON generation and parsing
- Markdown generation
- Japanese prompt generation
- English prompt generation
- Storage cleanup target selection by count
- Storage cleanup target selection by size
- Saving reports when the root folder does not exist
- Handling an invalid output root that is a file

## Covered By Submission Validation

- Settings default loading and validation
- Automatic capture setting behavior
- `Debug.LogError` capture through the recorder
- `NullReferenceException` capture through `Debug.LogException`
- report ID and fingerprint generation
- `report.json`, `report.md`, `prompt_ja.txt`, and `prompt_en.txt` generation
- Privacy Sanitizer exit behavior on generated prompt/report data
- duplicate report suppression
- duplicate occurrence counting
- report history loading
- report deletion
- corrupt `report.json` skip
- max report count cleanup
- max storage size deletion target selection
- output folder creation
- invalid output root safe failure
- simulated Domain Reload callback duplicate prevention
- Editor Window menu registration
- demo sample isolation and expected trigger labels
- batchmode screenshot fallback without blocking report generation
- settings persistence across a new Unity process
- report history loading across a new Unity process

## Latest Local Results

Last run: 2026-08-02 after the public review cleanup.

Environment:

```text
Unity 6000.4.6f1
Windows Editor
Package com.yp.bugshot-ai 0.2.0
```

Original project:

```text
Batchmode compile: Pass
EditMode tests: 30 passed / 0 failed
Submission validation RunAll: 27 passed / 0 failed
Submission validation restart Phase 1: 2 passed / 0 failed
Submission validation restart Phase 2: 3 passed / 0 failed
```

Clean validation project:

```text
Project created with Unity -createProject
Local package reference: file:<absolute-path-to-repo>/Packages/com.yp.bugshot-ai
Package resolve and compile: Pass
Basic Setup sample import-equivalent compile: Pass
EditMode tests: 30 passed / 0 failed
Submission validation RunAll: 27 passed / 0 failed
Submission validation restart Phase 1: 2 passed / 0 failed
Submission validation restart Phase 2: 3 passed / 0 failed
Windows Player build smoke: Pass
```

Unity 2022.3 LTS: Not tested.

## Screenshot Notes

Batchmode has no visible Game View. In the current validation run, `ScreenCapture.CaptureScreenshotAsTexture()` returned no texture outside Play Mode and the recorder still saved the report with `screenshotError`.

Normal Editor screenshot content remains a Human Review item because it requires a visible Game View and visual inspection.

## Human Review Still Required

Use [QA_CHECKLIST.md](QA_CHECKLIST.md) for the short interactive pass. GUI layout, screenshot content, clipboard behavior, and light/dark theme readability still require a normal Editor session.

Unity 2022.3 LTS also requires the full compile, test, validation, and Player Build pass before the README can claim it as tested.
