# Player falls through the floor near the right platform

## Summary
In DemoScene, the player falls through the floor near the right platform after moving right, dashing, and jumping near the platform edge. BugShot AI captured the error, screenshot file, player position, environment, stack trace, and recent gameplay events.

## Environment
- Unity Version: 6000.4.6f1
- Platform: WindowsEditor
- Operating System: Windows 11  (10.0.26200)
- Graphics Device: NVIDIA GeForce RTX 5070 Laptop GPU
- Product Name: BugShotAI Demo
- Company Name: YP
- Package Version: 0.1.0

## Steps to Reproduce
1. Open `DemoScene`.
2. Enter Play Mode.
3. Press `D` or `Right Arrow` to move toward the right platform.
4. Press `Left Shift` to dash.
5. Press `Space` to jump near the platform edge.
6. Press `B` to trigger the demo bug.

## Expected Result
The player should stay on or above the right platform floor after moving, dashing, and jumping.

## Actual Result
The player Y position dropped below the expected floor height and Unity logged: `Player fell through the floor near the right platform.`

## Logs
- Log Type: Error
- Condition: `Player fell through the floor near the right platform.`
- Important stack trace line: `BugShotAIDemoBugTrigger:TriggerDemoBug () (at Assets/Samples/BugShotAI/BasicSetup/BugShotAIDemoBugTrigger.cs:63)`
- Recent events:
  - Pressed move right
  - Pressed dash
  - Pressed jump
  - Moved to right platform
  - Pressed dash before jump
  - Jumped near platform edge
  - Player Y position dropped below expected floor height

## Screenshot
bugshot_20260614_204818_125.png

## Severity
High