# Instructions for agents — ABC Invoicing project (Spec-First)

## Current phase: second exam — implementation of the UBL XML generation
Phase 1 (spec-first) is closed. Phase 2 implements, in the stack of ADR 0002 (C# .NET 10), the generation of the
Invoice XML from the UBL 2.1 XSD, its validation and its sending, with unit tests. Code lives only in:
- `src/Abc.Ubl/` — classes generated from `xsd/ubl-2.1` (ADR 0005); `Generated/` is never edited, only regenerated.
- `src/Abc.Invoicing.Domain`, `.Application`, `.Infrastructure` — the hexagonal core of ADR 0001 (dependency rule checked by `ArchitectureTests`).
- `tests/Abc.Invoicing.Tests/` — xUnit; one test per AC it covers, named `AcNN_…` or with the AC in `DisplayName`.
Do not create other projects (Web, MAUI, persistence) until the specs ask for them. `tools/` keeps verifying the specs.

## Before every commit
`dotnet test` and `python tools/validate_specs.py` must both end with 0 failed. If you change a contract, an example, the matrix or an XSD, run it; if you change examples, rebuild the reference XML with `python tools/ubl_build.py`; if you change a Mermaid diagram, run `python tools/render_diagrams.py`.

## Mandatory flow
1. Problem statement → `specs/problem-analysis.md`
2. `spec-writer` → `specs/features/00N-*/spec.md` (skill `write-spec`)
3. `sunat-expert` → `research.md` and `specs/traceability/regulatory-matrix.md` with sources
4. `spec-reviewer` → `review.md`; clarifications go to the human
5. `architect` → ADRs, `specs/architecture/`, `specs/domain/`
6. `migration-analyst` → `specs/migration/`
7. `contract-designer` → `specs/contracts/` (validates JSON and YAML)
8. `qa` → one test case per AC and `specs/traceability/traceability.md`

## Current decisions
- Architecture: MVC monolith with a hexagonal core — [ADR 0001](specs/adr/0001-architecture.md).
- Stack: .NET 10 (ASP.NET Core MVC + MAUI) + PostgreSQL — [ADR 0002](specs/adr/0002-technology-stack.md).
- XSD before signing — [ADR 0003](specs/adr/0003-validate-xsd-before-signing.md).
- MSI + APK — [ADR 0004](specs/adr/0004-msi-apk-packaging.md).
- UBL XML from classes generated from the XSD, never templates — [ADR 0005](specs/adr/0005-ubl-from-xsd-generated-classes.md).
- Certificate only on the server, encrypted, with rotation — [ADR 0006](specs/adr/0006-certificate-custody.md).
- Two transactions + outbox; CDR handled by return-code range — [ADR 0007](specs/adr/0007-two-step-issuance-outbox.md).
- Numbering and signing only on the server; one Series per point of issue — [ADR 0008](specs/adr/0008-server-numbering-series-per-point-of-issue.md).
- Amazonía IGV exemption as establishment configuration (proposed) — [ADR 0009](specs/adr/0009-amazonia-igv-exemption.md).
- Signature: in-place C14N of SignedInfo, ds: prefix — [ADR 0010](specs/adr/0010-signature-in-place-canonicalization.md).

## Rules for everyone
- Read `specs/00-constitution.md` and `specs/glossary.md` before any task.
- If something is not in the spec, do not invent it: ask or mark `[NEEDS CLARIFICATION]`.
- Every tax datum carries a source (URL) or the mark "to be verified".
- Continuous global numbering: FR, NFR, AC and US do not restart per feature.
- When changing an FR, update in the same change: AC, `traceability.md`, regulatory matrix (if applicable), schema, OpenAPI, examples and reference XML.
- SUNAT catalogs live as data in `specs/catalogs/`; a schema enum must be a subset of its catalog (the validator checks it).
