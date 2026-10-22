---
id: 0002
title: Technology stack — .NET 10 (ASP.NET Core MVC + .NET MAUI) and PostgreSQL
status: proposed
origin: criteria
---
# Context
Neither the user nor the problem statement fixes a language. Signals: UBL XML with XSD validation and XMLDSig signature (FR-07 to FR-09), SOAP with WS-Security to SUNAT (FR-10), MVC monolith (ADR 0001), and native clients on Windows with MSI and Android with APK (NFR-04). One language for server and clients allows sharing DTOs and validations.

# Decision
- **Server (monolith)**: C# on .NET 10 LTS with ASP.NET Core MVC. XSD with `XmlSchemaSet`; signature with `System.Security.Cryptography.Xml.SignedXml`; SOAP client generated from SUNAT's WSDL.
- **Clients**: .NET MAUI (Blazor Hybrid) — one codebase for Windows and Android. Windows: MSI installer built with WiX Toolset; Android: signed APK.
- **Database**: PostgreSQL (server). SQLite on the client only for the local queue and catalog cache.
- **Shared contracts**: `Abc.Contracts` library with DTOs generated from `contracts/schemas`.

# Alternatives evaluated
| Option | Domain fit (XML/XSD/signature) | NFR (native MSI + APK) | Integrations (SUNAT SOAP) | Simplicity | Result |
|---|---|---|---|---|---|
| **C# .NET 10 + MAUI** | High: XSD and XMLDSig in the standard library | Windows and Android from one codebase; MSI via WiX | Native SOAP | One language everywhere | **Chosen** |
| Java Spring Boot + Flutter | High on the server (JAXB, Santuario) | Flutter gives APK and Windows; MSI needs separate WiX | Good | Two languages (Java + Dart) | Rejected |
| Rust/TypeScript with Tauri 2 + Node | Medium: XMLDSig in JS is less mature | Tauri produces MSI (WiX) and APK natively | Medium | Medium | Rejected due to XML signature risk |

# Consequences
+ XSD validation, signature and SOAP without critical third-party libraries.
+ DTOs shared between client and server.
– MAUI produces MSIX by default; MSI requires a WiX project (see the `dotnet-maui` skill).
– The team must know C#.
