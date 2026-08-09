# Design Decisions

This document records decisions that came from the first implementation and later validation. It is not a list of planned features.

## Keep One Unity-Facing Recorder

The MVP put collection, formatting, and saving in `BugShotAIRecorder`. That made the first demo quick to build, but privacy and duplicate behavior could not be tested without exercising the component.

The recorder now keeps the Unity lifecycle work: callbacks, FPS, player Transform, screenshot request, and coordination. Deterministic rules moved out only when they gained a direct test or a separate failure boundary.

## Combine Output Formatting

JSON, Markdown, and prompt generation were initially separate classes. Each accepted the same report and returned text, so the separation added names without clarifying the flow. They now live in `BugShotAIReportFormatter`.

Storage remains separate because file creation, legacy loading, deletion, and cleanup fail for different reasons than string formatting.

## Keep Snapshot Collection in the Builder

`BugShotAIReportBuilder` reads the active Scene, `Application`, and `SystemInfo` while it creates one report snapshot. It does not sanitize, format, or save the result.

Environment collection could be moved behind another type, but there is currently no alternate implementation or independent recovery rule. Keeping the short Unity snapshot code with report construction makes the capture sequence easier to follow without adding another interface or wrapper.

## Sanitize Before Writing

Raw Unity logs can contain user directories, emails, Authorization values, and token-like assignments. The report model is sanitized before JSON, Markdown, or prompts are written. The Editor preview sanitizes again so edited or legacy data is not displayed as automatically safe.

This is pattern-based masking, not a guarantee. Screenshots and project-specific names still require human review.

## Use Fingerprints for Repeated Errors

The first version had no cooldown. A log emitted every frame could create many JSON and PNG files.

The fingerprint uses log type, condition, and one useful stack line. Timestamp, scene, FPS, and player position are intentionally excluded because including changing context would prevent identical errors from matching.

The duplicate tracker is separate because its time and occurrence-count rules can be tested without Unity callbacks.

Suppressed hits increase the tracker's in-memory count but do not rewrite the first saved JSON during the cooldown. The validation therefore checks one saved folder and the tracker count as separate facts.

## Let Reports Survive Screenshot Failure

`ScreenCapture.CaptureScreenshotAsTexture()` returned no texture in batchmode validation. A screenshot is useful but not required for the log and environment report, so capture returns bytes or an error string. Storage writes the remaining files and records the reason in `screenshotError`.

## Do Not Send Automatically

Automatic GitHub or AI submission was not included. It would add token storage, scopes, network errors, and a risk of sending unreviewed project data. The current version stops at local files and explicit copy actions.

## Keep Runtime and Editor Assemblies Separate

The Editor Window and settings UI are Editor-only. The recorder and report pipeline remain in Runtime so Play Mode capture is straightforward and public breadcrumb APIs do not depend on `UnityEditor`.

The cost is that the Runtime assembly enters Player builds. A Windows Player build smoke test passes on Unity 6000.4.6f1. If the project becomes strictly Editor-only, moving the runtime pipeline should be treated as an API decision rather than a cosmetic asmdef change.

## State Version Evidence Literally

The package was compiled, tested, validated in a clean local UPM project, and Player-built with Unity 6000.4.6f1 on Windows. The same pass has not been run with Unity 2022.3 LTS, so the README does not claim that version as tested.
