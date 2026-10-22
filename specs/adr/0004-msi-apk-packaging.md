---
id: 0004
title: Packaging — MSI for Windows and APK for Android
status: accepted
origin: problem statement
---
# Context
§2: "must mandatorily be packaged in a friendly installation format (MSI installer for desktop environments or an APK for Android distributions)".

# Decision
- Windows: publish the MAUI client as an unpackaged app and include it in an **MSI** installer built with WiX Toolset, with shortcut, clean uninstall and versioned upgrade.
- Android: release **APK** signed with ABC's own keystore (outside the repository, constitution 15).
- The server URL and environment are configured on first launch, not compiled in.

# Alternatives evaluated
| Option | Result |
|---|---|
| MSIX (MAUI's default format) | Rejected: the problem statement asks for MSI |
| Web application (PWA) | Rejected: the problem statement asks for native execution |

# Consequences
+ Meets the distribution requirement literally.
– Build pipeline with two outputs and code-signing management.
