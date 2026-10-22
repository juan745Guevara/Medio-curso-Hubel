---
name: sunat-einvoicing
description: Technical procedures for SUNAT electronic invoicing — validate RUC, calculate IGV, ISC (catalog 8), ICBPER and totals, submission deadlines and use of Annex 8 catalogs. Defines no new business rules; rules live in the specs.
---

# SUNAT electronic invoicing — procedures

Each procedure cites the spec rule it implements. Data verified on 22/10/2026; sources in `specs/features/*/research.md`.

## 1. Validate RUC (FR-02)
1. Strip spaces; require 11 numeric digits.
2. Prefix in {10, 15, 16, 17, 20}.
3. Sum = Σ digit[i] × weight[i] for the first 10, weights `5 4 3 2 7 6 5 4 3 2`.
4. r = 11 − (sum mod 11); if r = 10 → 0; if r = 11 → 1.
5. r must equal digit 11.

Example: 20131312955 → sum 94, 94 mod 11 = 6, 11 − 6 = 5 ✓.
Note: modulo 11 validates format, not existence or ACTIVE/HABIDO status (that requires the SUNAT taxpayer registry; see clarifications).

## 2. Calculate an Item (FR-05)
Mandatory order, with exact decimals (never floating point):

1. `saleValue = quantity × unitValue − itemDiscount`
2. ISC per catalog 08 (`cbc:TierRange`):
   - `01` Ad valorem: `isc = saleValue × iscRate`
   - `02` Fixed amount: `isc = quantity × fixedAmountPerUnit`
   - `03` Retail sale price: `isc = suggestedRetailPrice × factor × iscRate` (factor formula to be verified in Appendix IV lit. C and the SUNAT guide)
3. IGV per catalog 07:
   - `10`–`17` taxed: `igv = (saleValue + isc) × 0.18`
   - `20`–`21` exempt, `30`–`37` non-taxable, `40` export: `igv = 0`
4. ICBPER (if the Product is a plastic bag): `icbper = bags × currentAmount` (S/ 0.50 since 2023). Not part of the IGV base.
5. `itemAmount = saleValue + isc + igv + icbper`
6. `unitPrice = (saleValue + isc + igv) / quantity` (catalog 16 code `01`).

Check of AC-09: 100.00 × 25 % = 25.00; (100.00 + 25.00) × 18 % = 22.50; total 147.50.

## 3. Totals (FR-06)
- `taxed` = Σ saleValue of items 10–17; `exempt` = Σ 20–21; `nonTaxable` = Σ 30–37.
- `igv`, `isc`, `icbper` = Σ of the items.
- `totalAmount` = taxed + exempt + nonTaxable + igv + isc + icbper − globalDiscounts.
- Half-up rounding to 2 decimals at the end of each line and of the total. SUNAT's tolerance between the sum of lines and the total is in the validation Excel (to be verified).

## 4. IGV rate in 2026
The XML shows 18 (`cbc:Percent`). Internally, per Law 32387: 2026 = 15.5 % IGV + 2.5 % IPM; 2027 = 15 % + 3 %; 2028 = 14.5 % + 3.5 %; 2029 = 14 % + 4 %. The split only affects reports, not the XML or the total.

## 5. Most used Annex 8 catalogs

| Cat. | For | Values in scope |
|---|---|---|
| 01 | Document type | 01 invoice, 03 sales receipt, 07 CN, 08 DN |
| 02 | Currency | PEN, USD |
| 03 | Unit | NIU (unit), KGM, LTR, ZZ (service) |
| 05 | Tax | 1000 IGV (VAT), 2000 ISC (EXC), 7152 ICBPER (OTH), 9997 exempt, 9998 non-taxable, 9999 other |
| 06 | Identity document | 0 other, 1 DNI, 4 foreigner ID, 6 RUC, 7 passport, A diplomatic ID |
| 07 | IGV treatment | 10 taxed onerous, 20 exempt onerous, 30 non-taxable onerous, 40 export |
| **08** | **ISC system** | **01 ad valorem, 02 fixed amount, 03 retail sale price** |
| 09 | CN reason | 01–10, 13 |
| 10 | DN reason | 01 interest, 02 value increase, 03 penalties |
| 16 | Price type | 01 price with IGV, 02 reference value (free) |
| 51 | Operation type | 0101 domestic sale |
| 52 | Legends | 1000 amount in words |

Load catalogs as data with an effective date (constitution 9), not as constants in the code.

## 6. Deadlines (FR-11, FR-16, FR-24, FR-25)
| Document | Deadline |
|---|---|
| Invoice and its notes | Up to 3 calendar days counted from the day after issuance |
| Sales receipt daily summary | Up to the 7th calendar day after |
| Voiding notice | 7 calendar days from the day after |
| Contingency | Report up to the 7th calendar day after |

Implement the deadlines in a single service (`SunatDeadlines`) that receives the date from `ClockPort` (America/Lima time zone).

## 7. File names
`<RUC>-<TT>-<SERIES>-<NUMBER>.xml`, zipped into a `.zip` with the same name. Summary: `<RUC>-RC-<YYYYMMDD>-<n>`. Voiding: `<RUC>-RA-<YYYYMMDD>-<n>`.

## 8. Before sending: SUNAT errors prevented locally
Validate in this order and return the internal code; the full table is in `specs/traceability/regulatory-matrix.md` §G.
1. RUC and Buyer document (prevents 2016, 2017, 2800, 2802).
2. Payment terms and installments (prevents 3244–3267).
3. Recalculate each line and the totals with procedure §2–§3 (prevents 4287, 4288, 4290, 4301, 4309, 4211, 4321).
4. Catalog 08 only with tax 2000 (prevents 2041, 2373, 3210).
5. XSD (prevents 1002, 1010, 2073, 2075).
6. Sign and verify the signature without reformatting (prevents 1059, 2335, 2336).

## 9. Amount in words (legend 1000)
Written in Spanish, as SUNAT requires: `<integer in words> CON <cents>/100 <CURRENCY>`, upper case: 79.00 PEN → "SETENTA Y NUEVE CON 00/100 SOLES".
