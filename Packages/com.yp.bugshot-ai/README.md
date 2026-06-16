# BugShot AI for Unity

## What is BugShot AI?

BugShot AI is a Unity package that records debugging context when an `Error`, `Exception`, or `Assert` is logged.

It is not a bug detection system. Instead, it helps preserve the situation around an error so that developers can inspect it later or paste the report into an AI assistant to draft a GitHub Issue.

Generated reports include a JSON file, a PNG screenshot, environment details, optional player position, and recent gameplay breadcrumbs recorded from code.

## Demo

Demo flow:

1. Enter Unity Play Mode.
2. Press `D -> LeftShift -> Space -> B` in the demo sample.
3. A floor-clipping style error is logged to the Console.
4. BugShot AI saves `bugshot_*.json`, `bugshot_*.png`, and a prompt Markdown file.
5. The JSON report includes `recentEvents`.
6. The saved prompt can be used to generate a GitHub Issue draft.

Demo video:

[BugShot AI demo video](Documentation~/demo/bugshot-ai-demo-42s.mp4)

Editor Window:

![BugShot AI Editor Window](Documentation~/images/bugshot-ai-window.png)

Reports Folder:

![BugShot AI Reports Folder](Documentation~/images/reports-folder.png)

JSON report with `recentEvents`:

![BugShot AI JSON recentEvents](Documentation~/images/bugshot-recent-events-json.png)

Generated GitHub Issue example:

![BugShot AI generated GitHub Issue](Documentation~/images/bugshot-generated-github-issue.png)

## Features

- Detects Unity `Error`, `Exception`, and `Assert` logs via `Application.logMessageReceived`
- Saves a JSON report under `Application.persistentDataPath/BugShotAI/`
- Saves a PNG screenshot next to the JSON report
- Records scene name, scene path, FPS, optional player position, and environment information
- Stores recent gameplay breadcrumbs through `BugShotAIEventLogger.Record(category, message)`
- Provides an Editor Window at `Tools > BugShot AI > Open Window`
- Creates a `BugShotAIRecorder` in the active scene from the Editor Window
- Opens the reports folder from the Editor Window
- Copies or saves Japanese and English GitHub Issue prompts from the latest JSON report

## Installation

### Option 1: Copy Into Packages

Copy this package folder into your Unity project:

```text
Packages/com.yp.bugshot-ai/
```

Then open or reload the Unity project.

### Option 2: Git URL

After publishing this repository, the package can be installed through Unity Package Manager using a Git URL that points to the package path:

```text
https://github.com/ypkabu/bugshot-ai-unity.git?path=Packages/com.yp.bugshot-ai
```

## Basic Setup

1. Open Unity.
2. Select `Tools > BugShot AI > Open Window`.
3. Click `Create BugShotAI Recorder In Scene`.
4. Optional: select the created `BugShotAI` GameObject and assign your player object to `Player Transform`.
5. Enter Play Mode.
6. Trigger an error or exception.

Reports are saved to:

```text
Application.persistentDataPath/BugShotAI/
```

Use `Open Reports Folder` in the Editor Window to open the folder.

### Trigger A Test Error

In Play Mode, open `Tools > BugShot AI > Open Window` and click `Trigger Test Error`.

You can also trigger an error from gameplay code:

```csharp
using UnityEngine;

public sealed class ExampleError : MonoBehaviour
{
    private void Start()
    {
        Debug.LogError("Test error");
    }
}
```

## Demo Bug Trigger Sample

The package includes a sample script:

```text
Samples~/BasicSetup/BugShotAIDemoBugTrigger.cs
```

This sample records a short floor-clipping bug sequence for demos and README screenshots.

Setup:

1. Import or copy the Basic Setup sample into your project.
2. Add `BugShotAIDemoBugTrigger` to any active GameObject in the scene.
3. Make sure a `BugShotAIRecorder` exists in the scene.
4. Enter Play Mode.

Input sequence:

1. Press `D` or `Right Arrow` to record a move-right event.
2. Press `LeftShift` to record a dash event.
3. Press `Space` to record a jump event.
4. Press `B` to trigger the demo bug.

When `B` is pressed, the sample records these breadcrumbs and logs an error:

```text
Player fell through the floor near the right platform.
```

## JSON Report Example

Example report structure:

```json
{
  "timestampUtc": "2026-06-14T20:48:18.1300610Z",
  "sceneName": "DemoScene",
  "scenePath": "Assets/Scenes/DemoScene.unity",
  "logType": "Error",
  "condition": "Player fell through the floor near the right platform.",
  "stackTrace": "UnityEngine.Debug:LogError (object)\nBugShotAIDemoBugTrigger:TriggerDemoBug () ...",
  "screenshotPath": ".../BugShotAI Demo/BugShotAI/bugshot_20260614_204818_125.png",
  "screenshotFileName": "bugshot_20260614_204818_125.png",
  "fps": 66.03,
  "playerPosition": {
    "hasPlayer": true,
    "x": 2.5,
    "y": -3.2,
    "z": 0.0
  },
  "environment": {
    "unityVersion": "6000.4.6f1",
    "platform": "WindowsEditor",
    "operatingSystem": "Windows 11",
    "deviceModel": "Unknown",
    "systemMemorySize": 32485,
    "graphicsDeviceName": "NVIDIA GeForce RTX 5070 Laptop GPU",
    "productName": "BugShotAI Demo",
    "companyName": "YP",
    "packageVersion": "0.1.0"
  },
  "recentEvents": [
    {
      "category": "Player",
      "message": "Pressed move right"
    },
    {
      "category": "Player",
      "message": "Pressed dash"
    },
    {
      "category": "Player",
      "message": "Pressed jump"
    },
    {
      "category": "Bug",
      "message": "Player Y position dropped below expected floor height"
    }
  ]
}
```

Report fields:

- `timestampUtc`: UTC timestamp for the report
- `sceneName`: active scene name
- `scenePath`: active scene asset path when available
- `logType`: Unity log type
- `condition`: log message
- `stackTrace`: Unity stack trace
- `screenshotPath`: screenshot path
- `screenshotFileName`: PNG file name only
- `fps`: smoothed FPS value
- `playerPosition`: optional player position
- `environment`: Unity, device, project, and package information
- `recentEvents`: recent breadcrumbs recorded with `BugShotAIEventLogger`

## GitHub Issue Prompt

After a report is generated, open `Tools > BugShot AI > Open Window`.

Available actions:

- `Copy GitHub Issue Prompt JP`
- `Copy GitHub Issue Prompt EN`
- `Save GitHub Issue Prompt JP To File`
- `Save GitHub Issue Prompt EN To File`

The prompt asks an AI assistant to generate a Markdown GitHub Issue with:

- Title
- Summary
- Environment
- Steps to Reproduce
- Expected Result
- Actual Result
- Logs
- Screenshot
- Severity

The prompt also instructs the AI assistant not to invent missing information. Missing values should be written as `Unknown`.

If you shorten the JSON for a README, video, or social post, keep the fields that support the issue content you expect AI to generate. Do not ask AI to include details that are not present in the shortened JSON.

## Verification Status

- Configured for Unity 2022.3 or later
- Tested on Unity 6000.4.6f1 with Windows Editor
- Unity 2022.3 LTS verification is pending
- Runtime and Editor assemblies are separated
- Runtime code does not reference `UnityEditor`

## Roadmap

- In-Editor issue preview
- Configurable report fields
- Report list viewer
- Optional integrations for external issue trackers
