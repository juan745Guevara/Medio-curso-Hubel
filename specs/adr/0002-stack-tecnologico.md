---
id: 0002
title: Stack tecnológico — .NET 10 (ASP.NET Core MVC + .NET MAUI) y PostgreSQL
status: propuesta
origen: criterio
---
# Contexto
Ni el usuario ni el enunciado fijan lenguaje. Señales: XML UBL con validación XSD y firma XMLDSig (RF-07 a RF-09), SOAP con WS-Security hacia SUNAT (RF-10), monolito MVC (ADR 0001), y clientes nativos en Windows con MSI y Android con APK (RNF-04). Un mismo lenguaje en servidor y clientes permite compartir DTOs y validaciones.

# Decisión
- **Servidor (monolito)**: C# sobre .NET 10 LTS con ASP.NET Core MVC. XSD con `XmlSchemaSet`; firma con `System.Security.Cryptography.Xml.SignedXml`; cliente SOAP generado del WSDL de SUNAT.
- **Clientes**: .NET MAUI (Blazor Hybrid) — un código para Windows y Android. Windows: paquete instalable MSI con WiX Toolset; Android: APK firmado.
- **Base de datos**: PostgreSQL (servidor). SQLite en el cliente solo para cola local y caché de catálogos.
- **Contratos compartidos**: biblioteca `Abc.Contracts` con los DTO generados de `contracts/schemas`.

# Alternativas evaluadas
| Opción | Ajuste al dominio (XML/XSD/firma) | RNF (MSI + APK nativos) | Integraciones (SOAP SUNAT) | Simplicidad | Resultado |
|---|---|---|---|---|---|
| **C# .NET 10 + MAUI** | Alto: XSD y XMLDSig en la librería estándar | Windows y Android con un código; MSI vía WiX | SOAP nativo | Un lenguaje en todo | **Elegida** |
| Java Spring Boot + Flutter | Alto en servidor (JAXB, Santuario) | Flutter da APK y Windows; MSI requiere WiX aparte | Bueno | Dos lenguajes (Java + Dart) | Descartada |
| Rust/TypeScript con Tauri 2 + Node | Medio: firma XMLDSig menos madura en JS | Tauri genera MSI (WiX) y APK nativamente | Medio | Media | Descartada por el riesgo en firma XML |

# Consecuencias
+ Validación XSD, firma y SOAP sin librerías de terceros críticas.
+ DTOs compartidos cliente–servidor.
– MAUI genera MSIX por defecto; el MSI requiere un proyecto WiX (ver skill `dotnet-maui`).
– El equipo debe conocer C#.
