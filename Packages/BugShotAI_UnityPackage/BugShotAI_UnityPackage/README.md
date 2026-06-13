# BugShot AI for Unity

BugShot AI for Unity is an MVP Unity package that captures useful debugging context when an Error, Exception, or Assert appears in the Unity Console.

## MVP Features

- Detects `Error` and `Exception` logs via `Application.logMessageReceived`
- Saves a screenshot on error
- Saves a JSON report under `Application.persistentDataPath/BugShotAI/`
- Includes:
  - UTC timestamp
  - active scene name
  - log type
  - condition
  - stack trace
  - screenshot path
  - FPS
  - optional player position
  - recent event breadcrumbs

## Installation

1. Copy this package folder into your Unity project, for example:
   `Packages/com.yp.bugshot-ai/`
2. Open Unity 2022.3 LTS or later.
3. Create an empty GameObject named `BugShotAI`.
4. Add `BugShotAIRecorder` to the GameObject.
5. Optional: assign your Player Transform in the Inspector.

## Usage

Add breadcrumbs from gameplay code:

```csharp
using YP.BugShotAI;

BugShotAIEventLogger.Record("Player", "Entered Stage 1");
```

Trigger a test error:

```csharp
Debug.LogError("Test error");
```

Reports are saved to:

```text
Application.persistentDataPath/BugShotAI/
```

## Next Roadmap

- Editor setup window
- Open reports folder button
- AI issue summary generation
- GitHub Issue markdown export
- Discord notification
