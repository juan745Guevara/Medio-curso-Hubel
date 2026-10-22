---
id: 0001
title: Architecture — MVC monolith with a hexagonal core
status: accepted
origin: problem statement
---
# Context
The problem statement requires (§2): "built initially under a clean monolithic architecture using the MVC pattern… decoupled so as to allow an easy future migration toward a Hexagonal or Clean Architecture, preparing to scale to microservices… this detail must be clearly visible in the specs". There are also complex regulations (FR-05, FR-07 to FR-10) and a swappable external integrator (SUNAT or OSE, NFR-07).

# Decision
**MVC monolith with the Model organized as a hexagonal core.**

- **V (View)**: Windows and Android client apps, plus Razor admin views on the server.
- **C (Controller)**: monolith controllers. They only translate HTTP ↔ DTO and call an **input port**. They hold no rules.
- **M (Model)**: split into three layers with inward dependencies: `Domain` (entities, value objects, domain services), `Application` (use cases = input ports; output ports as interfaces) and `Infrastructure` (adapters: database, UBL, XSD, signature, SUNAT/OSE, PDF, mail).

Dependency rule (verified with architecture tests):

| Package | May depend on | May not depend on |
|---|---|---|
| Domain | nothing | Application, Infrastructure, Web, frameworks |
| Application | Domain | Infrastructure, Web |
| Infrastructure | Application, Domain | Web |
| Web (Controllers + Views) | Application | Domain directly, Infrastructure (only via composition) |

This is the rule shared by Hexagonal and Clean Architecture; the layer-by-layer equivalence is in [architecture.md §4b](../architecture/architecture.md#4b-hexagonal-or-clean-the-same-dependency-rule).

So "migrating to hexagonal" means renaming Web and Infrastructure as adapters, without touching Domain or Application; and "migrating to microservices" means splitting by bounded context (Invoicing, Catalog, Inventory, Migration, Registers), which no longer call each other internally but through ports and events.

# Alternatives evaluated
| Option | Domain fit | NFR | Integrations | Simplicity | Result |
|---|---|---|---|---|---|
| Classic MVC (model = ORM entities, logic in controllers) | Low: tax rules mixed with HTTP and database | Meets | Switching SUNAT to OSE touches controllers | High | Rejected: violates "decouple" |
| **MVC with hexagonal core** | High: pure, testable domain | Meets | Ports for SUNAT/OSE | Medium | **Chosen** |
| Microservices from day 1 | High | Overkill for a mid-sized company; more operations | High | Low | Rejected: the problem statement asks for an initial monolith |

# Consequences
+ Tax calculation and rules are tested without a database or SUNAT.
+ Switching OSE, or from SUNAT to OSE, is a new adapter.
+ Microservice boundaries already exist as bounded contexts.
– More projects and DTO ↔ domain mappings than a classic MVC.
– Requires discipline: architecture tests that fail if Domain imports Infrastructure.
