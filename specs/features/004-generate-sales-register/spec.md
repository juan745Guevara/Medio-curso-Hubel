# Spec 004 — Generate sales register and stock card

## Context
RS 234-2006/SUNAT requires formal books and registers. With the system's Electronic documents and Inventory movements, ABC can produce the Period's Sales register and the per-Product stock card (*kardex*), which it does not keep today. Both follow the electronic-book structures of RS 286-2009/SUNAT (PLE): format **14.1** Registro de Ventas e Ingresos (book 140100) and format **12.1** Registro del Inventario Permanente en Unidades Físicas (book 120100).

## Actors
- **Accountant**: exports and reviews registers.
- **Administrator**.

## User stories
- US-15: As an Accountant, I want the Period's Sales register in format 14.1 with all Electronic documents to reconcile with SUNAT.
- US-16: As an Administrator, I want the stock card per Product in format 12.1 (inflows, outflows, balance) from the first migrated movement.

## Functional requirements
- **FR-37** (Event-driven): When the Accountant requests the Sales register of a Period, the system shall list each Electronic document of the Period with the 35 fields of SUNAT PLE format 14.1 (Registro de Ventas e Ingresos, RS 108-2020/SUNAT Annex IV) in their order, filled from the UBL origin defined in `regulatory-matrix.md` §I.
- **FR-38** (Ubiquitous): The system shall subtract credit notes and add debit notes in the Sales register of the Period in which they are issued, filling fields 28 to 31 (date, Table 10 type, Series and number of the modified document).
- **FR-39** (Unwanted behaviour): If an Electronic document of the Period is in PENDING_RESEND or REJECTED, then the system shall flag it in the Register, exclude it from the totals and leave it out of the TXT export.
- **FR-40** (Ubiquitous): The system shall show a Product's stock card for a Period and establishment with the 19 fields of SUNAT PLE format 12.1 (Registro del Inventario Permanente en Unidades Físicas, RS 042-2018/SUNAT Annex 2) — establishment, Table 5 stock type, Table 6 unit, Table 10 document type with Series and number, Table 12 operation type, inflow, outflow — plus the running balance after each Inventory movement, including migrated ones.
- **FR-41** (Event-driven): When the Accountant exports the Sales register, the system shall generate the PLE TXT file named `LE<RUC><AAAAMM00>140100<00><1><content><currency>1.txt` (content 1 with data, 0 without; currency 1 PEN, 2 USD), one line per row with fields 1–35 separated and terminated by `|`, dates as DD/MM/AAAA and amounts with 2 decimals.
- **FR-42** (Event-driven): When the Accountant exports the inventory register of a Period, the system shall generate the PLE TXT file `LE<RUC><AAAAMM00>120100<00><1><content><currency>1.txt` with fields 1–19 of format 12.1 for every Product and establishment, with the first row of the first Period in operation being the opening balance (Table 12 code 16) equal to the reconciled legacy balance.
- **FR-43** (Unwanted behaviour): If a Product with movements in the Period has no Table 5 stock type or its unit has no Table 6 code, then the system shall block the 12.1 export with `PLE_INCOMPLETE_PRODUCT` and list the Products to complete.
- **FR-44** (Ubiquitous): The system shall assign field 35 of format 14.1 as follows: `1` for ACCEPTED or OBSERVED documents of the Period; `2`, with all amounts at 0.00, for documents VOIDED in the Period; `8` for documents of a previous Period accepted after that Period was exported.

## Non-functional requirements
- **NFR-12**: The Sales register of a Period with 20,000 Electronic documents shall be generated in ≤ 30 s.

## Acceptance criteria
AC-48 (FR-37) — Period register
  Given 3 ACCEPTED Documents in 2026-10
  When the Accountant requests the 2026-10 Register
  Then they see 3 rows, each with the 35 fields of format 14.1

AC-49 (FR-38) — Credit note subtracts
  Given an Invoice of 118.00 and a credit note of 18.00 in the same Period
  When totals are generated
  Then the net amount is 100.00

AC-50 (FR-39) — Rejected excluded
  Given a REJECTED Document in the Period
  When the Register is generated
  Then it appears flagged, does not add up and is not written to the TXT

AC-51 (FR-40) — Stock card with migrated data
  Given Product P-001 with a migrated inflow of 50 and an outflow of 10 from a Document
  When the Administrator opens its stock card
  Then they see 2 movements and a final balance of 40

AC-52 (FR-41) — Export
  Given the 2026-10 Register with 3 rows
  When the Accountant exports it
  Then they get a TXT with 3 lines, each with 35 fields separated by "|" and ending in "|"

AC-53 (FR-37) — Format 14.1 fields of an invoice
  Given Invoice F001-00000042 issued on 15/10/2026 to RUC 20131312955 with taxed base 100.00, IGV 18.00 and total 118.00 in PEN
  When the 2026-10 Register is generated
  Then the row has period 20261000, type 01, series F001, number 42, customer document type 6 and number 20131312955, taxed base 100.00, IGV 18.00, total 118.00, currency PEN, exchange rate 1.000 and status 1

AC-54 (FR-38) — Modified document of a credit note
  Given credit note FC01-00000003 issued on 20/10/2026 that modifies Invoice F001-00000042 of 15/10/2026 by 18.00
  When the 2026-10 Register is generated
  Then the note row has type 07, total -18.00 and fields 28–31 = 15/10/2026, 01, F001, 42

AC-55 (FR-37) — Document in dollars
  Given an Invoice for USD 100.00 + IGV issued on a day whose SUNAT exchange rate is 3.745
  When the Register is generated
  Then the row has currency USD and exchange rate 3.745

AC-56 (FR-40) — Format 12.1 fields with running balance
  Given P-001 (stock type 01, unit NIU) in establishment 0000 with an opening balance of 50 and Invoice F001-00000042 for 10 units
  When the 2026-10 stock card is opened
  Then row 1 has operation 16, inflow 50.00, outflow 0.00 and balance 50.00, and row 2 has document 01 F001 42, operation 01, inflow 0.00, outflow -10.00 and balance 40.00

AC-57 (FR-41) — PLE file name of the Sales register
  Given Issuer RUC 20131312955 and Period 2026-10 with data in PEN
  When the Register is exported
  Then the file is named LE2013131295520261000140100001111.txt

AC-58 (FR-42) — Inventory register export
  Given the 2026-10 movements of AC-56
  When the inventory register is exported
  Then the file LE2013131295520261000120100001111.txt has 2 lines with 19 fields each, dates as DD/MM/AAAA

AC-59 (FR-43) — Product without stock type
  Given Product P-009 with movements in 2026-10 and no Table 5 stock type
  When the inventory register is exported
  Then the export is blocked with PLE_INCOMPLETE_PRODUCT listing P-009

AC-60 (FR-44) — Voided document in the register
  Given Invoice F001-00000045 for 118.00 VOIDED in 2026-10
  When the 2026-10 Register is generated
  Then its row has status 2 and every amount is 0.00

## Out of scope
- Uploading the TXT to SUNAT (the Accountant does it with the PLE program or SIRE).
- Valued stock card (format 13.1), because the legacy data has no costs.
- Purchase register and accounting books.

## Clarifications
- [NEEDS CLARIFICATION] What tax regime does ABC have and which books must it keep under RS 234-2006?
- [NEEDS CLARIFICATION] Is ABC obliged to keep the Sales register in SIRE (RVIE)? The 14.1 structure is generated in any case and is the base of the RVIE proposal.
- [NEEDS CLARIFICATION] Must the stock card be valued (weighted average cost) or only in units? The legacy data has no costs.
