# Player fell through the floor near the right platform.

## Summary

- Report ID: ExampleReport
- Timestamp UTC: 2026-07-31T00:00:03Z
- Log Type: Error
- Scene: DemoScene
- Scene Path: Assets/Scenes/DemoScene.unity
- Fingerprint: demo1234abcd5678
- Occurrence Count: 3

## Environment

- Unity Version: 6000.4.6f1
- Platform: WindowsEditor
- OS: Windows 11
- Graphics: Demo GPU
- Product Name: BugShotAI Demo
- Company Name: YP
- BugShot Version: 0.2.0

## Steps to Reproduce

Press D, LeftShift, Space, then B in DemoScene.

## Expected Result

The player should remain on the right platform.

## Actual Result

The player Y position dropped below the expected floor height.

## Notes

Sanitizer demo input included dummy user paths, email, token, URL fragment token, and IP address.

## Logs

- Condition: `Player fell through the floor near the right platform.`
- Stack Trace:
```text
UnityEngine.Debug:LogError (object)
BugShotAIDemoBugTrigger:TriggerDemoBug () (at <USER_HOME>/SecretProject/Assets/Samples/BugShotAI/BasicSetup/BugShotAIDemoBugTrigger.cs:63)
DemoService:SendReport () (at <USER_HOME>/SecretProject/Assets/DemoService.cs:24)
LinuxRunner:Execute () (at <USER_HOME>/SecretProject/Assets/LinuxRunner.cs:18)
NetworkShare:Read () (at <UNC_PATH>\SecretProject\Assets\NetworkShare.cs:7)
Authorization: <REDACTED> api_key= <REDACTED> access_token= <REDACTED>
https://example.invalid/callback#access_token= <REDACTED>
Contact: <EMAIL>
IP: <IP_ADDRESS>

```
- Recent Events:
  - [Player] Pressed move right
  - [Player] Pressed dash
  - [Player] Pressed jump
  - [Player] Moved to right platform
  - [Player] Pressed dash before jump
  - [Player] Jumped near platform edge
  - [Bug] Player Y position dropped below expected floor height
- Recent Console Logs:
  - [Log] Loaded config from <USER_HOME>/SecretProject/config.json with token= <REDACTED>
  - [Error] Player fell through the floor near the right platform.

## Screenshot

ExampleReport/screenshot.png
