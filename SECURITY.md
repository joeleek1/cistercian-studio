# Download and publisher information

The Windows widget is an unsigned .NET Framework application. The project has not configured a trusted publisher code-signing certificate or Microsoft Store distribution. Version information and SHA-256 checksums identify a build; they do not replace a trusted signature or certify that software is safe.

## Browser and Windows warnings

An unknown-publisher or unrecognized-app warning can arise from missing signing/reputation. Chrome also distinguishes unfamiliar files from downloads detected as dangerous. The user-reported warning's exact wording was unavailable, so its cause has not been confirmed.

- [Microsoft: SmartScreen reputation for Windows app developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)
- [Google: Chrome blocks some downloads](https://support.google.com/chrome/answer/6261569?hl=en)
- [Microsoft: code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)

Do not disable security protection or add exclusions to run this app. If a warning names a virus or malware threat, stop and record the exact detection and file hash. A clean scan from one engine is not a guarantee, and it does not establish browser or SmartScreen reputation.

Trusted Authenticode signing requires a publisher identity and certificate or signing service. Signing can identify the publisher, but a new signed app may still need to build reputation. Store distribution is another route. Renaming the ZIP, adding version metadata, or making a self-signed certificate does not solve publisher trust.

## Verify a downloaded package

Compare its SHA-256 hash with the version-matched checksum in `dist/downloads`. `release-info.json` records the executable and archive hashes, version and Authenticode status. For example:

```powershell
Get-FileHash -LiteralPath .\CistercianClock-v1.1.0-Windows.zip -Algorithm SHA256
```

Source and the build script are public in `desktop`. The program draws the clock, reads local time, and saves its display preferences beside the executable. It does not include a network client, installer, startup-registration step, updater, or elevated execution request.

To report a problem, include the app version, warning text, detection name if present, and SHA-256 hash in a repository issue. Do not include credentials or personal system logs.
