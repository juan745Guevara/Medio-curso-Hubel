---
name: dotnet-aspnetcore
description: C# / .NET 10 and ASP.NET Core MVC best practices applied to the invoicing monolith with a hexagonal core (ADR 0001 and 0002).
---

# .NET 10 + ASP.NET Core MVC in this project

1. **Thin controllers.** A controller receives the DTO (`Abc.Contracts`), calls an `Application` use case and returns `ProblemDetails` on errors. Zero tax rules, zero `DbContext` in controllers. Check that each action has at most one line of business logic.

2. **Always `decimal` for money.** Never `double` or `float` (constitution 7). Round with `Math.Round(x, 2, MidpointRounding.AwayFromZero)` encapsulated in the `Money` value object, not scattered.

3. **Enforced dependency rule.** `Architecture.Tests` project with NetArchTest or ArchUnitNET: "Domain does not depend on Microsoft.EntityFrameworkCore, Microsoft.AspNetCore or Infrastructure". The build fails if it breaks.

4. **XML and signature with the standard library.** `System.Xml.Schema.XmlSchemaSet` for XSD and `System.Security.Cryptography.Xml.SignedXml` for XMLDSig, behind `XsdValidatorPort` and `SignerPort`. Never reformat the XML after signing (`PreserveWhitespace = true`).

5. **Background work and resilience.** Resends, daily summary and ticket queries as `BackgroundService` (or Hangfire if a dashboard is needed). The SUNAT SOAP client with Polly: 30 s timeout (AC-19), retries with backoff and circuit breaker; when retries are exhausted → PENDING_RESEND.

6. **Secrets outside the code.** `dotnet user-secrets` in development; in production, encrypted environment variables or a secret store. The .pfx is loaded from there (NFR-05).

7. **RFC 9457 errors.** `AddProblemDetails()` and a single mapping of domain exceptions → stable `code` (`INVALID_RUC`, `INVALID_XSD`…) matching `invoicing-api.yaml`.
