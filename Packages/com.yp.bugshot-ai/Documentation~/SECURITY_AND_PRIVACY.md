# Security and Privacy

BugShot AI is designed to help developers share useful debugging context without accidentally exposing local paths or secrets.

## What Is Masked

The sanitizer masks:

- Windows user directories such as `C:\Users\name`
- macOS user directories such as `/Users/name`
- Linux home directories such as `/home/name`
- UNC user paths such as `\\BuildShare\Users\name`
- The current Unity project absolute path
- Email addresses
- `Authorization` headers
- `Bearer` tokens
- GitHub token-like strings
- API key, access token, client secret, secret, and token assignments
- URL query or fragment values named `token`, `key`, `secret`, `api_key`, `access_token`, or `client_secret`
- IP addresses when the setting is enabled

Specific project and home roots are replaced before the broader path patterns. This preserves `<PROJECT_ROOT>` when possible instead of reducing every matching path to `<USER_HOME>`.

Example using dummy test data:

```text
Before: C:\Users\alice\Project\Assets\Test.cs
After:  <USER_HOME>\Project\Assets\Test.cs

Before: Authorization: Bearer demo-token
After:  Authorization: <REDACTED>
```

## Output Policy

BugShot AI saves sanitized reports. The JSON, Markdown, and prompt files are generated from the sanitized report model.

The Editor Window displays safe path labels by default. Full local paths are still available through explicit copy actions when a developer needs them locally.

## No External Sending

The package does not send reports to external services.

GitHub Issue API posting and external AI API calls are intentionally not implemented in this version. This avoids storing tokens in the Unity Editor and avoids sending unreviewed debugging data outside the local machine.

## Remaining Risks

- Unity stack traces or user-provided notes may contain project-specific names that are not secrets but could still be sensitive.
- Screenshots may contain sensitive in-game or desktop content.
- IP masking is optional because local IPs can be useful during networking debugging.
- A custom output directory can point anywhere the user selects. The package does not upload that directory.
- Broad path and token patterns can mask harmless text. The current policy prefers a false positive in shared report text over leaving a likely credential visible.

## Recommended Demo Practice

- Use the Editor Window safe path display for screenshots and videos.
- Review `report.json`, `report.md`, and prompt files before publishing.
- Do not publish raw reports from a private project without checking screenshots and user notes.
- Use `Documentation~/ExampleReport/` for public README examples when possible.
