---
name: dotnet-aspnetcore
description: Buenas prácticas de C# / .NET 10 y ASP.NET Core MVC aplicadas al monolito de facturación con núcleo hexagonal (ADR 0001 y 0002).
---

# .NET 10 + ASP.NET Core MVC en este proyecto

1. **Controladores delgados.** Un controlador recibe el DTO (`Abc.Contracts`), llama a un caso de uso de `Application` y devuelve `ProblemDetails` en errores. Cero reglas tributarias, cero `DbContext` en controladores. Revisa que cada acción tenga una línea de negocio como mucho.

2. **`decimal` siempre para dinero.** Nunca `double` ni `float` (constitución 7). Redondeo con `Math.Round(x, 2, MidpointRounding.AwayFromZero)` encapsulado en el value object `Dinero`, no disperso.

3. **Regla de dependencias comprobada.** Proyecto `Architecture.Tests` con NetArchTest o ArchUnitNET: "Domain no depende de Microsoft.EntityFrameworkCore, Microsoft.AspNetCore ni Infrastructure". Falla el build si se rompe.

4. **XML y firma con la librería estándar.** `System.Xml.Schema.XmlSchemaSet` para XSD y `System.Security.Cryptography.Xml.SignedXml` para XMLDSig, detrás de `ValidadorXsdPort` y `FirmadorPort`. Nunca reformatear el XML después de firmar (`PreserveWhitespace = true`).

5. **Trabajo en segundo plano y resiliencia.** Reenvíos, resumen diario y consulta de tickets como `BackgroundService` (o Hangfire si se necesita panel). El cliente SOAP de SUNAT con Polly: timeout 30 s (CA-19), reintentos con backoff y circuito abierto; al agotar reintentos → PENDIENTE_REENVIO.

6. **Secretos fuera del código.** `dotnet user-secrets` en desarrollo; en producción, variables de entorno cifradas o un almacén de secretos. El .pfx se carga desde ahí (RNF-05).

7. **Errores RFC 9457.** `AddProblemDetails()` y un mapeo único de excepciones de dominio → `code` estable (`RUC_INVALIDO`, `XSD_INVALIDO`…) igual al de `facturacion-api.yaml`.
