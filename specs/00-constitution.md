# System constitution

Invariants that apply to the whole system. No spec, ADR or task may contradict them; changing one requires a new ADR.

## Process
1. No feature is implemented without an approved `spec.md` and its row in `traceability/traceability.md`.
2. Every tax rule cites its source (regulation, annex or catalog) in `research.md` or in `traceability/regulatory-matrix.md`.
3. Anything not in the problem statement or in a cited source is marked `[NEEDS CLARIFICATION]`; it is never assumed.

## Tax domain
4. No electronic document is sent to SUNAT or the OSE without first passing, in this order: business validation, validation against the UBL 2.1 XSD, and digital signature.
5. An issued document (with a number assigned) is immutable. It is only corrected with a credit note, a debit note or a voiding notice.
6. An assigned sequence number is never reused, even if the document is rejected or voided.
7. Every amount is calculated and stored in the domain with exact decimal precision; never floating point.
8. A Factura (invoice) is only issued to a Buyer identified with a valid RUC.
9. Catalog codes used in a document come from the current version of the Annex 8 catalogs loaded into the system, with an effective date.
10. The signed XML, the CDR and the printed representation are kept unaltered for at least 5 years.

## Data and legacy
11. Every migrated record keeps the identifier and the content of its source row (traceability to the legacy system).
12. A historical legacy movement is never turned into an issued electronic document.

## Architecture
13. The domain does not depend on frameworks, database, user interface or the SUNAT/OSE provider. Dependencies point toward the domain.
14. All communication with external systems goes through a port declared in `contracts/ports.md`.

## Security
15. No secret (certificate, certificate password, SOL key, OSE token) is versioned in the repository.
16. Error responses to the user never expose internal traces; they do expose the SUNAT/OSE code and description when there is one.
