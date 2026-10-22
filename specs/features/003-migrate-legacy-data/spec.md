# Spec 003 — Migrate legacy data

## Context
The current software stores Legacy records with fecha, ingreso, salida, producto and nro_documento (date, inflow, outflow, product, document number). Management requires all that history to move into the new entities "without losing traceability". Those records are not Documents and cannot become Documents (constitution, invariant 12).

## Actors
- **Administrator**: runs and approves the migration.
- **Auditor**: reviews the migration report.

## User stories
- US-12: As an Administrator, I want to import the warehouse history so no information is lost.
- US-13: As an Auditor, I want to trace each new record back to its source row.
- US-14: As an Administrator, I want to see which rows were rejected and why, to fix them and retry.

## Functional requirements
- **FR-28** (Event-driven): When the Administrator uploads a legacy export file, the system shall create a Migration batch with date, user, file name, number of rows and file hash.
- **FR-29** (Ubiquitous): The system shall turn each valid Legacy record into an Inventory movement: type INFLOW if ingreso > 0, OUTFLOW if salida > 0, with the record's date and product.
- **FR-30** (Ubiquitous): The system shall store in each migrated movement the Migration batch identifier, the source row number and the original content of the Legacy record.
- **FR-31** (Event-driven): When a Legacy record product does not exist in the Product catalog, the system shall create it in PENDING_CLASSIFICATION status, with no IGV treatment assigned, for the Administrator to complete.
- **FR-32** (Unwanted behaviour): If a Legacy record has an invalid date, empty product, both inflow and outflow at zero or both greater than zero, or negative values, then the system shall reject it and log the reason.
- **FR-33** (Unwanted behaviour): If a Legacy record was already migrated in a previous batch (same content and row), then the system shall skip it and count it as a duplicate.
- **FR-34** (Ubiquitous): The system shall produce a reconciliation report per batch: rows read = migrated + rejected + duplicates, and balance per Product computed from the legacy data equal to the migrated balance.
- **FR-35** (Ubiquitous): The system shall keep nro_documento as the movement's internal reference, never as the Series or Sequence number of a Document.
- **FR-36** (State-driven): While a Product is in PENDING_CLASSIFICATION, the system shall prevent its use in a Document.

## Non-functional requirements
- **NFR-10**: Migrating 100,000 Legacy records shall take ≤ 10 minutes.
- **NFR-11**: An interrupted migration shall be resumable without duplicating rows.

## Acceptance criteria
AC-39 (FR-28) — Batch recorded
  Given a file of 1,200 rows
  When the Administrator uploads it
  Then there is a Migration batch with 1,200 rows and the file hash

AC-40 (FR-29) — Inflow
  Given the record {fecha 2025-03-04, ingreso 50, salida 0, producto "Arroz Costeño 5kg", nro_documento "G-0091"}
  When it is migrated
  Then an INFLOW movement of 50 of Product "Arroz Costeño 5kg" dated 2025-03-04 is created

AC-41 (FR-30) — Traceability to the row
  Given the movement created in AC-40
  When the Auditor checks its origin
  Then they see the batch, row 17 and the five original fields

AC-42 (FR-31) — New product
  Given a product "Aceite Primor 1L" that does not exist
  When its record is migrated
  Then the Product is created in PENDING_CLASSIFICATION

AC-43 (FR-32) — Invalid record
  Given a record with ingreso 10 and salida 5 in the same row
  When it is migrated
  Then it is rejected with reason "simultaneous inflow and outflow"

AC-44 (FR-33) — Retry without duplicates
  Given an already migrated batch
  When the Administrator uploads the same file
  Then 0 new rows are migrated and all are counted as duplicates

AC-45 (FR-34) — Reconciliation
  Given a batch of 1,200 rows with 1,180 migrated and 20 rejected
  When the report is generated
  Then 1,180 + 20 + 0 = 1,200 and each Product's balance matches the one computed from the legacy data

AC-46 (FR-35) — nro_documento is not a document
  Given a record with nro_documento "F001-123"
  When it is migrated
  Then it is stored as an internal reference and no Document F001-00000123 exists

AC-47 (FR-36) — Unclassified product
  Given a Product in PENDING_CLASSIFICATION
  When the Salesperson adds it to an Invoice
  Then the system blocks it with "Product without tax treatment"

## Out of scope
- Rebuilding historical sales as Documents or regularizing past periods with SUNAT.
- Migrating prices or customers (the problem statement does not say they exist in the legacy data).

## Clarifications
- [NEEDS CLARIFICATION] In what format can the legacy data be exported (CSV, Excel, database)?
- [NEEDS CLARIFICATION] Are "ingreso" and "salida" quantities or amounts? In which unit?
- [NEEDS CLARIFICATION] Is "producto" a free-text name or a code? Is the same product written in several ways?
- [NEEDS CLARIFICATION] Is nro_documento unique? What does it represent (dispatch guide, paper receipt, internal voucher)?
- [NEEDS CLARIFICATION] How many years of history and roughly how many rows are there?
