# Instructions for agents — ABC Invoicing project (Spec-First)

## Current phase: specification. No code is written.
Do not create `backend/`, `frontend/`, `src/` or code files. Only `specs/`, `.claude/` and the root files are edited.

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

## Rules for everyone
- Read `specs/00-constitution.md` and `specs/glossary.md` before any task.
- If something is not in the spec, do not invent it: ask or mark `[NEEDS CLARIFICATION]`.
- Every tax datum carries a source (URL) or the mark "to be verified".
- Continuous global numbering: FR, NFR, AC and US do not restart per feature.
- When changing an FR, update in the same change: AC, `traceability.md`, regulatory matrix (if applicable), schema and OpenAPI.
