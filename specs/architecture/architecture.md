# Architecture: MVC with a Hexagonal vision

Deliverable 2 of the exam. Decisions in [ADR 0001](../adr/0001-architecture.md), [ADR 0002](../adr/0002-technology-stack.md), [ADR 0003](../adr/0003-validate-xsd-before-signing.md) and [ADR 0004](../adr/0004-msi-apk-packaging.md).

The idea in one line: **today it is an MVC monolith; its "M" is already built as a hexagon**, so the future migration does not rewrite rules, it only relocates adapters.

## 1. C4 level 1 — Context

```mermaid
flowchart TB
  user([Salesperson / Administrator / Accountant])
  buyer([Buyer])
  sys[ABC e-invoicing system]
  sunat[(SUNAT<br/>billService)]
  ose[(OSE)]
  legacy[(Legacy software<br/>warehouse records)]
  mail[(Mail server)]
  user -- uses --> sys
  sys -- signed UBL XML / CDR --> sunat
  sys -. alternative .-> ose
  legacy -- one-time export --> sys
  sys -- PDF + XML --> mail --> buyer
```

## 2. C4 level 2 — Containers

```mermaid
flowchart LR
  subgraph Native clients
    win[Windows app<br/>.NET MAUI · MSI]
    and[Android app<br/>.NET MAUI · APK]
    cache[(Local SQLite<br/>queue + catalogs)]
  end
  subgraph ABC server
    web[ASP.NET Core MVC monolith<br/>Controllers + admin Views]
    jobs[Scheduled jobs<br/>resends, daily summary, tickets]
    db[(PostgreSQL)]
    files[(XML, CDR, PDF storage<br/>5 years)]
  end
  win & and -- HTTPS JSON<br/>invoicing-api.yaml --> web
  win & and --- cache
  web --- db
  jobs --- db
  web --- files
  web -- SOAP WS-Security --> sunat[(SUNAT / OSE)]
  jobs -- SOAP --> sunat
```

## 3. Initial view: MVC (what is built first)

```mermaid
flowchart TB
  subgraph V[View]
    v1[MAUI app: sales, notes, lookup screens]
    v2[Razor views: configuration, migration, registers]
  end
  subgraph C[Controller]
    c1[DocumentsController]
    c2[NotesController]
    c3[VoidsController / SummariesController]
    c4[MigrationsController]
    c5[RegistersController]
  end
  subgraph M[Model]
    direction TB
    app[Application<br/>use cases = input ports]
    dom[Domain<br/>ElectronicDocument, Item, TaxCalculator,<br/>RucValidator, IssuancePolicy, SunatDeadlines]
    inf[Infrastructure<br/>Repositories, UblGenerator, XsdValidator,<br/>Signer, SunatClient, PDF, Mail]
  end
  V --> C
  C -- DTO --> app
  app --> dom
  inf -. implements ports of .-> app
```

Clean MVC rules the design requires:

1. The controller does not calculate, does not validate tax rules and does not touch the database: it maps the DTO and calls a use case.
2. The view never receives domain entities; it receives response DTOs.
3. The model knows nothing about HTTP, MAUI, PostgreSQL or SUNAT.

## 4. Target view: Hexagonal (ports and adapters)

```mermaid
flowchart LR
  subgraph Primary adapters
    a1[REST API MVC]
    a2[Razor views]
    a3[Scheduled jobs]
    a4[Automated tests]
  end
  subgraph Hexagon
    pin[[Input ports<br/>IssueDocument, IssueNote,<br/>SubmitVoiding, GenerateDailySummary,<br/>MigrateBatch, GenerateSalesRegister]]
    core((Domain<br/>Invoicing · Catalog · Inventory))
    pout[[Output ports<br/>DocumentRepository, SeriesRepository,<br/>UblGenerator, XsdValidator, Signer,<br/>SunatValidator, Notifier, Clock]]
    pin --> core --> pout
  end
  subgraph Secondary adapters
    b1[PostgreSQL]
    b2[UBL 2.1 + OASIS XSD]
    b3[XMLDSig + certificate]
    b4[SUNAT billService]
    b5[OSE X]
    b6[SMTP]
  end
  a1 & a2 & a3 & a4 --> pin
  pout --> b1 & b2 & b3 & b4 & b5 & b6
```

Details of each port in [`contracts/ports.md`](../contracts/ports.md).

## 4b. Hexagonal or Clean: the same dependency rule

The problem statement asks to migrate "toward a Hexagonal Architecture or Clean Architecture". Both share the central rule: dependencies point toward the domain. This design serves both without changing core code; only the layer names change.

| Proposed project | MVC (today) | Hexagonal ([Cockburn](https://alistaircockburn.com/Articles/Hexagonal-Architecture)) | Clean Architecture (Robert C. Martin) | Examples in this system |
|---|---|---|---|---|
| `Abc.Invoicing.Domain` | Model (core) | Inside the hexagon: domain | **Entities** | `ElectronicDocument`, `Item`, `Ruc`, `Money`, `TaxCalculator`, `IssuancePolicy` |
| `Abc.Invoicing.Application` | Model (services) | Input ports (use cases) and output port definitions | **Use Cases** (interactors) and gateway interfaces | `IssueDocument`, `IssueNote`, `MigrateBatch`; `SunatValidatorPort`, `SignerPort` |
| `Abc.Invoicing.Web` (Controllers) | **Controller** | Primary (driving) adapters | **Interface Adapters**: controllers and presenters | `DocumentsController`, DTO ↔ use case mapping |
| `Abc.Invoicing.Web` (Views), `Abc.Client.Maui` | **View** | Primary adapters | **Frameworks & Drivers**: UI | Admin Razor views, Windows/Android app |
| `Abc.Invoicing.Infrastructure` | Model (data access) | Secondary (driven) adapters | **Interface Adapters** (gateways, repositories) + **Frameworks & Drivers** (DB, web) | `PostgresDocumentRepository`, `Ubl21Generator`, `OasisXsdValidator`, `SunatSoapClient`, `OseClient` |
| `Abc.Contracts` | View DTOs | Input port format | Request / Response models | `IssueDocumentRequest`, `ElectronicDocumentResponse` |

```mermaid
flowchart LR
  subgraph FD[Frameworks & Drivers]
    UI[MAUI app · Razor views]
    DB[(PostgreSQL)]
    SUNAT[(SUNAT / OSE)]
  end
  subgraph IA[Interface Adapters]
    CTRL[MVC Controllers]
    GW[Repositories · SUNAT/OSE client · UBL generator · Signer]
  end
  subgraph UC[Use Cases]
    UCS[IssueDocument · IssueNote · SubmitVoiding · MigrateBatch]
  end
  subgraph EN[Entities]
    DOM[ElectronicDocument · TaxCalculator · Ruc · Money]
  end
  UI --> CTRL --> UCS --> DOM
  GW -. implements ports of .-> UCS
  GW --> DB & SUNAT
```

Arrows only cross the circles inward: `Entities` does not know `Use Cases`, and `Use Cases` knows neither the database nor SUNAT. It is the same dependency rule as [ADR 0001](../adr/0001-architecture.md), verified by an architecture test on every build.

## 5. Migration path

| Step | From | To | What changes | What does NOT change |
|---|---|---|---|---|
| 0 | — | MVC monolith with hexagonal core | Built this way from the start | — |
| 1 | Controllers + Infrastructure | Explicitly named primary and secondary adapters | Rename projects; register adapters by configuration | Domain, Application, API contracts |
| 2 | Internal calls between contexts | Domain events (`DocumentAccepted` → Inventory) | `EventsPort` moves from in-memory to a broker | Use cases |
| 3 | One deployment | Services per context: Invoicing, Catalog, Inventory, Registers | Each context with its own database and API | Ports and DTOs |
| 4 | SUNAT submission inside Invoicing | Isolated "SUNAT/OSE Gateway" service | `SunatValidatorPort` adapter becomes an HTTP client of the gateway | Invoicing domain |

```mermaid
flowchart LR
  m[MVC monolith<br/>hexagonal core] --> h[Modular monolith<br/>explicit hexagonal] --> e[Contexts with events] --> ms[Microservices<br/>Invoicing · Catalog · Inventory · Registers · SUNAT Gateway]
```

## 6. Sequence of the central flow (issue invoice)

```mermaid
sequenceDiagram
  autonumber
  actor V as Salesperson
  participant App as MAUI app
  participant C as DocumentsController
  participant UC as IssueDocument
  participant D as Domain
  participant S as SeriesRepository
  participant U as UblGenerator
  participant X as XsdValidator
  participant F as Signer
  participant VS as SunatValidator
  V->>App: Confirms sale
  App->>C: POST /documents (DTO)
  C->>UC: execute(request)
  UC->>D: IssuancePolicy + RucValidator + TaxCalculator
  alt rule violated
    D-->>C: business error
    C-->>App: 422 problem+json
  end
  UC->>S: nextSequence(F001)
  UC->>U: generate(document)
  U-->>UC: UBL 2.1 XML
  UC->>X: validate(XML)
  alt invalid XSD
    X-->>UC: errors
    UC-->>C: INVALID_XSD (status DRAFT)
  end
  UC->>F: sign(XML)
  F-->>UC: signed XML + hash
  UC->>VS: sendBill(ZIP)
  alt CDR received
    VS-->>UC: CDR (accepted / observed / rejected)
  else no response
    UC-->>UC: PENDING_RESEND + retry
  end
  UC-->>C: ElectronicDocumentResponse
  C-->>App: 201 + status + PDF/XML/CDR links
```

## 7. Proposed folder structure (described, not created in this phase)

```
src/
  Abc.Invoicing.Domain/          # entities, value objects, domain services — no dependencies
  Abc.Invoicing.Application/     # use cases (input ports) and output port interfaces
  Abc.Invoicing.Infrastructure/  # EF Core/PostgreSQL, UBL, XSD, XMLDSig, SUNAT/OSE client, PDF, SMTP
  Abc.Invoicing.Web/             # ASP.NET Core MVC: Controllers, Views (admin), DI composition
  Abc.Contracts/                 # DTOs generated from specs/contracts/schemas (shared)
  Abc.Client.Maui/               # Windows (MSI) and Android (APK) app
  Abc.Installer.Wix/             # WiX project for the MSI
tests/
  Abc.Invoicing.Domain.Tests/        # one test per calculation and rule AC
  Abc.Invoicing.Architecture.Tests/  # ADR 0001 dependency rule
  Abc.Invoicing.Integration.Tests/   # XSD, signature, SUNAT beta
```

## 8. Which FRs each component covers

| Component | FR |
|---|---|
| TaxCalculator | FR-05, FR-06 |
| RucValidator, IssuancePolicy | FR-02, FR-03, FR-04, FR-12, FR-36 |
| SeriesRepository | FR-01 |
| UblGenerator + AmountInWordsConverter | FR-07, FR-15 |
| XsdValidator | FR-08 |
| Signer | FR-09 |
| SunatValidator + Scheduled jobs | FR-10, FR-11, FR-23, FR-25, FR-26 |
| NotePolicy | FR-18 to FR-22 |
| PrintedRepresentation + Notifier | FR-13, FR-14 |
| Migration | FR-28 to FR-35 |
| Registers | FR-37 to FR-41 |
