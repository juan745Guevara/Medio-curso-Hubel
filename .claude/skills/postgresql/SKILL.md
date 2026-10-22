---
name: postgresql
description: PostgreSQL best practices to persist electronic documents, sequence numbers, migrated movements and registers, aligned with the project constitution.
---

# PostgreSQL in this project

1. **Atomic sequence number.** `UPDATE series SET last_sequence = last_sequence + 1 WHERE code = $1 RETURNING last_sequence;` in the same transaction that inserts the document. Never `MAX()+1` (AC-01, AC-02). Unique index `(issuer_ruc, type, series, sequence_number)`.

2. **Money in `numeric(14,2)`** and unit values in `numeric(22,10)`. Never `real`/`double precision`.

3. **Immutability.** Issued documents without `UPDATE` of business fields: a trigger rejects changes except status and CDR columns; the signed XML is stored with its SHA-256 (`bytea` or file + hash).

4. **Legacy traceability.** `inventory_movement` table with `batch_id`, `source_row`, `original_content jsonb` and `row_hash` with a unique index to de-duplicate (FR-30, FR-33).

5. **Catalogs with effective dates.** `sunat_catalog(number, code, description, valid_from, valid_to)`; query by issue date, not by "latest".
