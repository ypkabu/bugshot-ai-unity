# QA Checklist

Use this short pass before publishing a package update or recording a demo.

## Automated

- [ ] Unity batchmode compile exits with code 0.
- [ ] EditMode tests pass.
- [ ] Submission validation and both restart phases pass.
- [ ] Windows Player build smoke succeeds.
- [ ] README links resolve.
- [ ] `git diff --check` passes for the package.
- [ ] Public files contain no real user path, email, token, or validation-project path.

Commands and expected output files are listed in [TESTING.md](TESTING.md).

## Editor

- [ ] `Tools > BugShot AI > Open Window` opens without Console errors.
- [ ] The window is readable in both Unity Personal and Pro themes.
- [ ] `Create Recorder In Scene` creates one recorder and selects an existing recorder on the second click.
- [ ] Play Mode test error creates one report folder.
- [ ] Error, Environment, Screenshot, Reproduction, Privacy, and Export sections are usable.
- [ ] Screenshot preview is not blank and contains no private visual content.
- [ ] JSON, Markdown, EN prompt, and JP prompt contain masked data.
- [ ] Duplicate Error Burst does not create one folder per log.
- [ ] Full paths appear only after an explicit copy or open action.

## Demo

- [ ] Import the Basic Setup sample and add `BugShotAIDemoBugTrigger` or `BugShotAIDemoErrorPanel` to a GameObject in any test Scene.
- [ ] Run `D -> LeftShift -> Space -> B` with `BugShotAIDemoBugTrigger`, or click `Debug.LogError` on `BugShotAIDemoErrorPanel`.
- [ ] Confirm the demo Error and recent event sequence.
- [ ] Show one generated report, the screenshot, masked preview, and copied Markdown or prompt.
- [ ] Review every captured frame for personal paths, account names, notifications, email, tokens, and IP addresses.

Verified Editor: Unity 6000.4.6f1 on Windows. Unity 2022.3 LTS has not been tested.
