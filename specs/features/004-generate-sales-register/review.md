# Review 004 — Sales register and stock card

| # | Location | Finding | Severity | Correction applied |
|---|---|---|---|---|
| 1 | FR-41, AC-52 | The export format depended on a clarification; the AC only checked one row per document | Medium | **Resolved 25/10:** PLE TXT of format 14.1 (FR-41, AC-52, AC-57) |
| 2 | Context | The books required by RS 234-2006 depend on ABC's regime, which the problem statement does not give | Medium | Explicit clarification |
| 3 | FR-37, `SalesRegisterRow` | Format 14.1 was partial: period AAAAMM00, CUO, due date, export value, other charges, currency, exchange rate and the full modified document (date and Table 10 type) were missing, and the schema was not linked to the PLE structure | High | **Resolved 25/10:** the schema has the 35 fields in order, each citing its PLE field; UBL origin per field in the matrix §I; AC-53 to AC-55, AC-60 |
| 4 | FR-40, stock card | Format 12.1 was partial: establishment, Table 5 stock type, Table 6 unit (with equivalence from Catalog 03), Table 10 document with series and number, Table 12 operation type and the balance per movement were missing | High | **Resolved 25/10:** `stock-card.schema.json` with the 19 fields + running balance; matrix §J and §K; FR-42, FR-43; AC-56, AC-58, AC-59 |

Rest of the checklist approved.
