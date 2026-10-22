---
name: dotnet-maui
description: .NET MAUI (Blazor Hybrid) best practices for the native Windows + Android client, including MSI packaging with WiX and the signed APK (ADR 0002 and 0004).
---

# .NET MAUI in this project

1. **One codebase, two platforms, zero rules.** The client only displays and captures: it uses the `Abc.Contracts` DTOs and calls the API. It may pre-validate format (RUC modulo 11) for UX, but the validation that counts is the server's.

2. **MSI on Windows (ADR 0004).** MAUI produces MSIX by default. For MSI:
   - Publish unpackaged: `<WindowsPackageType>None</WindowsPackageType>` and `dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:WindowsAppSDKSelfContained=true`.
   - `Abc.Installer.Wix` project (WiX Toolset v5/v6) taking the published folder: `ProductCode` per version, fixed `UpgradeCode`, Start menu shortcut and clean uninstall.
   - Sign the MSI and executable with ABC's code-signing certificate.

3. **APK on Android.** `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` with `AndroidKeyStore=true` and the keystore outside the repository (pipeline variables).

4. **Offline without losses.** Local SQLite for a request queue with `Idempotency-Key` (prevents double issuance on retry) and a cache of SUNAT catalogs and products. The queue never assigns sequence numbers: only the server does (constitution 6).

5. **Printing and sharing.** Download the PDF (A4 or 80 mm ticket) from the API and use MAUI `Share`/`Launcher`; thermal printing via the Windows driver or the manufacturer's app on Android (printer model to be clarified).
