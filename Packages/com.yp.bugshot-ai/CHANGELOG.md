# Changelog

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
- Unity 2022.3 LTS verification is pending.
