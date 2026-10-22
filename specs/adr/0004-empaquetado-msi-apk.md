---
id: 0004
title: Empaquetado — MSI para Windows y APK para Android
status: aceptada
origen: enunciado
---
# Contexto
§2: "debe empaquetarse obligatoriamente en un formato de instalación amigable (instalador MSI para entornos de escritorio o un APK para distribuciones Android)".

# Decisión
- Windows: publicar el cliente MAUI como aplicación sin empaquetar (unpackaged) e incluirlo en un instalador **MSI** construido con WiX Toolset, con acceso directo, desinstalación limpia y actualización por versión.
- Android: **APK** de release firmado con keystore propio de ABC (fuera del repositorio, constitución 15).
- La URL del servidor y el ambiente se configuran en el primer arranque, no se compilan.

# Alternativas evaluadas
| Opción | Resultado |
|---|---|
| MSIX (formato por defecto de MAUI) | Descartada: el enunciado pide MSI |
| Aplicación web (PWA) | Descartada: el enunciado pide ejecución nativa |

# Consecuencias
+ Cumple literalmente el requisito de distribución.
– Pipeline de build con dos salidas y gestión de firmas de código.
