# BugShot AI for Unity

BugShot AI is a local Unity Editor extension that saves the context around an Error or Exception as a report with logs, scene information, recent events, and an optional screenshot.

It does not find or fix bugs, and it does not send reports outside the local machine. Its purpose is to leave useful evidence for debugging and for drafting an Issue.

For the CA Game Gym portfolio, ECHO//SHIFT is the main game project. BugShot AI is a supporting work that shows Unity Editor tooling, debugging, failure handling, and testing.

## Project Links

- [Package documentation and demo](Packages/com.yp.bugshot-ai/README.md)
- [CA Game Gym portfolio supplement](Docs/Application/CA_Game_Gym/BugShotSummary.md)
- [Architecture](Packages/com.yp.bugshot-ai/Documentation~/ARCHITECTURE.md)
- [Testing](Packages/com.yp.bugshot-ai/Documentation~/TESTING.md)
- [Privacy notes](Packages/com.yp.bugshot-ai/Documentation~/SECURITY_AND_PRIVACY.md)

The installable UPM package is located at `Packages/com.yp.bugshot-ai/`.

## Verification

- Verified with Unity 6000.4.6f1 on Windows Editor
- Unity 2022.3 LTS: Not tested
- Runtime and Editor assemblies are separated
- Runtime code does not reference `UnityEditor`
