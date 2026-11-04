# Instructions for agents — ABC Invoicing project (Spec-First)

## Current phase: specification. No product code is written.
Do not create `backend/`, `frontend/` or `src/`. Only `specs/`, `.claude/`, `xsd/` (official schemas, never edited) and the root files are edited. The only code allowed is `tools/`: scripts that **verify** the specs (validator, reference XML builder, diagram exporter), never product code.

## Before every commit
`python tools/validate_specs.py` must end with 0 failed. If you change a contract, an example, the matrix or an XSD, run it; if you change examples, rebuild the reference XML with `python tools/ubl_build.py`; if you change a Mermaid diagram, run `python tools/render_diagrams.py`.

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

## Rules for everyone
- Read `specs/00-constitution.md` and `specs/glossary.md` before any task.
- If something is not in the spec, do not invent it: ask or mark `[NEEDS CLARIFICATION]`.
- Every tax datum carries a source (URL) or the mark "to be verified".
- Continuous global numbering: FR, NFR, AC and US do not restart per feature.
- When changing an FR, update in the same change: AC, `traceability.md`, regulatory matrix (if applicable), schema, OpenAPI, examples and reference XML.
- SUNAT catalogs live as data in `specs/catalogs/`; a schema enum must be a subset of its catalog (the validator checks it).
