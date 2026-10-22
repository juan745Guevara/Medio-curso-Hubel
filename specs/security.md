# Security

| Asset | Risk | Control | Ref. |
|---|---|---|---|
| Digital certificate (.pfx) and its password | Theft ⇒ fraudulent issuance in ABC's name | Stored encrypted on the server; never on clients or in the repository; only the Administrator uploads it; alert 30 days before expiry | NFR-05, constitution 15 |
| SOL user and key / OSE token | Access to SUNAT on ABC's behalf | Secret manager or encrypted environment variable; never in logs | constitution 15 |
| Signed XML and CDR | Tampering ⇒ loss of validity | SHA-256 hash stored; write-once storage for 5 years | NFR-06 |
| API | Unauthorized use | JWT with Salesperson, Administrator, Accountant roles; HTTPS mandatory | invoicing-api.yaml |
| Android keystore and code-signing certificate | Counterfeit installers | Outside the repository; used only in the release pipeline | ADR 0004 |
| Buyer data | Personal data exposure (Law 29733) | Minimum necessary; e-mail optional | to be verified |

| Role | May |
|---|---|
| Salesperson | Issue documents, look up, download, resend |
| Administrator | Everything the Salesperson does + notes, voids, configuration, migration |
| Accountant | Registers and stock card (read-only) |

`[NEEDS CLARIFICATION]` Final role matrix (see clarifications in 002).
