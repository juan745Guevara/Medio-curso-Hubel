# Spec 004 — Generate sales register and stock card

## Context
RS 234-2006/SUNAT requires formal books and registers. With the system's Electronic documents and Inventory movements, ABC can produce the Period's Sales register and the per-Product stock card (*kardex*), which it does not keep today.

## Actors
- **Accountant**: exports and reviews registers.
- **Administrator**.

## User stories
- US-15: As an Accountant, I want the Period's Sales register with all Electronic documents to reconcile with SUNAT.
- US-16: As an Administrator, I want the stock card per Product (inflows, outflows, balance) from the first migrated movement.

## Functional requirements
- **FR-37** (Event-driven): When the Accountant requests the Sales register of a Period, the system shall list each Electronic document of the Period with date, type, Series, Sequence number, Buyer's Identity document, base per IGV treatment, IGV, ISC, ICBPER, total amount and SUNAT status.
- **FR-38** (Ubiquitous): The system shall subtract credit notes and add debit notes in the Sales register of the Period in which they are issued.
- **FR-39** (Unwanted behaviour): If an Electronic document of the Period is in PENDING_RESEND or REJECTED, then the system shall flag it in the Register and exclude it from the totals.
- **FR-40** (Ubiquitous): The system shall show a Product's stock card with each Inventory movement (date, type, quantity, supporting document, running balance), including migrated ones.
- **FR-41** (Event-driven): When the Accountant exports the Sales register, the system shall generate a file with the structure defined in the corresponding clarification.

## Non-functional requirements
- **NFR-12**: The Sales register of a Period with 20,000 Electronic documents shall be generated in ≤ 30 s.

## Acceptance criteria
AC-48 (FR-37) — Period register
  Given 3 ACCEPTED Documents in 2026-10
  When the Accountant requests the 2026-10 Register
  Then they see 3 rows with all the fields of FR-37

AC-49 (FR-38) — Credit note subtracts
  Given an Invoice of 118.00 and a credit note of 18.00 in the same Period
  When totals are generated
  Then the net amount is 100.00

AC-50 (FR-39) — Rejected excluded
  Given a REJECTED Document in the Period
  When the Register is generated
  Then it appears flagged and does not add up

AC-51 (FR-40) — Stock card with migrated data
  Given Product P-001 with a migrated inflow of 50 and an outflow of 10 from a Document
  When the Administrator opens its stock card
  Then they see 2 movements and a final balance of 40

AC-52 (FR-41) — Export
  Given the 2026-10 Register
  When the Accountant exports it
  Then they get a file with one row per Electronic document

## Out of scope
- Filing the register in SIRE or PLE.
- Purchase register and accounting books.

## Clarifications
- [NEEDS CLARIFICATION] What tax regime does ABC have and which books must it keep under RS 234-2006?
- [NEEDS CLARIFICATION] Must the Sales register be exported in the SIRE (RVIE) structure or is an internal report enough?
- [NEEDS CLARIFICATION] Must the stock card be valued (weighted average cost) or only in units? The legacy data has no costs.
