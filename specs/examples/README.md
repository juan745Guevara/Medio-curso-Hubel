# Examples: valid, invalid and reference XML

Executable part of the specs. `python tools/validate_specs.py` (groups C, D and E) checks everything here on every change.

## `valid/` — requests that must be accepted
Each file is validated against its JSON Schema (by file prefix: `issue-` → `issue-document-request`, `note-` → `note-request`, `void-`, `series-`, `contingency-`, `legacy-`) **and** against the business rules that a schema cannot express ([`tools/sunat_calc.py`](../../tools/sunat_calc.py): RUC check digit, installments, receipts over S/ 700). `products.json` is the catalog the examples use; the ISC rates and amounts are illustrative until management answers the clarification of spec 001.

## `invalid/` — requests that must be rejected
[`invalid/expected.json`](invalid/expected.json) says, for every file, at which layer it must fail and why:

| File | Layer | Expected | AC |
|---|---|---|---|
| `issue-receipt-with-F-series` | schema 400 | `pattern` (03 ⇒ B) | AC-31 |
| `issue-invoice-to-dni` | schema 400 | `const` (01 ⇒ RUC) | AC-05 |
| `issue-credit-without-installments` | schema 400 | `required` | AC-21 |
| `issue-usd-without-exchange-rate` | schema 400 | `required` | AC-64 |
| `issue-free-line-with-value` | schema 400 | `pattern` (unit value 0.00) | AC-66 |
| `issue-free-line-without-reference-value` | schema 400 | `required` | AC-67 |
| `issue-unit-not-in-catalog-03` | schema 400 | `enum` | AC-08 |
| `issue-client-sends-taxes` | schema 400 | `additionalProperties` | AC-08 |
| `issue-client-sends-sequence-number` | schema 400 | `additionalProperties` | AC-01 |
| `issue-currency-not-allowed` | schema 400 | `enum` | — |
| `issue-invalid-ruc-check-digit` | business 422 | `INVALID_RUC` | AC-04 |
| `issue-installments-mismatch` | business 422 | `INSTALLMENTS_MISMATCH` | AC-22 |
| `issue-receipt-over-700-without-document` | business 422 | `RECEIPT_REQUIRES_DOCUMENT` | AC-06 |
| `product-isc-without-rate` | schema 400 | `required` | AC-09 |
| `product-isc-fixed-amount-with-rate` | schema 400 | `not` | AC-10 |
| `product-isc-system-not-in-catalog-08` | schema 400 | `enum` | AC-09 |
| `note-without-modified-document` | schema 400 | `required` | AC-30 |
| `note-without-reason` | schema 400 | `required` | AC-28 |
| `debit-note-with-credit-reason` | schema 400 | `enum` (catalog 10) | AC-28 |
| `note-series-letter-differs` | schema 400 | `pattern` (FR-20) | AC-31 |
| `series-wrong-letter` | schema 400 | `pattern` | AC-73 |
| `series-negative-sequence` | schema 400 | `minimum` | AC-74 |

## `xml/` — reference files, signed and valid against the official XSD
Built by [`tools/ubl_build.py`](../../tools/ubl_build.py) from the `valid/` requests, with classes generated from [`xsd/`](../../xsd/README.md) (ADR 0005), validated **before** and after signing (ADR 0003). The validator re-checks them against the XSD, verifies the XMLDSig signature with the certificate inside each file, and recalculates their amounts from the JSON request.

| File | Root / XSD | What it shows | AC |
|---|---|---|---|
| `20131312955-01-F001-42.xml` | Invoice | Cash; taxed + exempt line; legend 1000 | AC-08, AC-12, AC-13, AC-25 |
| `20131312955-01-F001-43.xml` | Invoice | Credit with 2 installments (`Cuota001`, `Cuota002`) | AC-21, AC-22 |
| `20131312955-01-F001-44.xml` | Invoice | ISC systems 01, 02 (per litre) and 03 in `TierRange`; ICBPER bag | AC-09, AC-11, AC-68 |
| `20131312955-01-F001-45.xml` | Invoice | USD with `PaymentExchangeRate` | AC-65 |
| `20131312955-03-B001-10.xml` | Invoice (03) | Sales receipt with a free-of-charge line (9996, catalog 16 code 02, legend 1002) | AC-67 |
| `20131312955-07-FC01-1.xml` | CreditNote | Return per item, `CreditNoteLine`, `BillingReference` | AC-29 |
| `20131312955-08-FD01-1.xml` | DebitNote | `RequestedMonetaryTotal`, `DebitNoteLine` | AC-75 |
| `20131312955-RC-20261020-1.xml` | SUNAT SummaryDocuments | Daily summary with `sac:` lines, condition code (catalog 19) | AC-36 |
| `20131312955-RA-20261021-1.xml` | SUNAT VoidedDocuments | Voiding notice of F001-43 | AC-76 |

The signing certificate is a self-signed **test** certificate created in memory on every build ("NOT FOR PRODUCTION"); its private key is never written to disk (constitution 15). The test signature uses RSA-SHA256 with C14N; the algorithm accepted in production is confirmed with SUNAT/the OSE when the real certificate is loaded (ADR 0006).
