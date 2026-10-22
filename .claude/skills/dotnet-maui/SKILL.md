---
name: dotnet-maui
description: Buenas prácticas de .NET MAUI (Blazor Hybrid) para el cliente nativo Windows + Android, incluido el empaquetado MSI con WiX y el APK firmado (ADR 0002 y 0004).
---

# .NET MAUI en este proyecto

1. **Un código, dos plataformas, cero reglas.** El cliente solo muestra y captura: usa los DTO de `Abc.Contracts` y llama a la API. Puede pre-validar formato (RUC por módulo 11) para UX, pero la validación que cuenta es la del servidor.

2. **MSI en Windows (ADR 0004).** MAUI produce MSIX por defecto. Para MSI:
   - Publicar sin empaquetar: `<WindowsPackageType>None</WindowsPackageType>` y `dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:WindowsAppSDKSelfContained=true`.
   - Proyecto `Abc.Instalador.Wix` (WiX Toolset v5/v6) que tome la carpeta publicada: `ProductCode` por versión, `UpgradeCode` fijo, acceso directo en el menú Inicio y desinstalación limpia.
   - Firmar MSI y ejecutable con el certificado de firma de código de ABC.

3. **APK en Android.** `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` con `AndroidKeyStore=true` y keystore fuera del repositorio (variables del pipeline).

4. **Sin conexión, sin pérdidas.** SQLite local para cola de solicitudes con `Idempotency-Key` (evita doble emisión al reintentar) y caché de catálogos SUNAT y productos. La cola nunca asigna correlativos: eso solo lo hace el servidor (constitución 6).

5. **Impresión y compartir.** Descargar el PDF (A4 o ticket 80 mm) desde la API y usar `Share`/`Launcher` de MAUI; impresión térmica vía driver de Windows o app del fabricante en Android (por clarificar el modelo de impresora).
