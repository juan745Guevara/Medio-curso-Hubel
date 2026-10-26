# Review 001 — Issue electronic document

Reviewed by `spec-reviewer` (self-review) on 22/10/2026 using the checklist in `.claude/agents/spec-reviewer.md`.

| # | Location | Finding | Severity | Correction applied |
|---|---|---|---|---|
| 1 | AC-11 | The result gave neither the IGV nor the Item amount: it could not be checked by hand | Medium | Added IGV = 0.05 and amount = 1.85 |
| 2 | FR-05, matrix row 35 | ICBPER tax code 7152 and tags `BaseUnitMeasure`/`PerUnitAmount` without a primary source | Medium | **Resolved 22/10:** 7152 confirmed in the Annexes of RS 150-2019; `PerUnitAmount` corrected to `cac:TaxCategory` |
| 3 | FR-05 (ISC system 03) | Formula of the factor on the retail sale price not confirmed | Medium | **Resolved 22/10:** factor = 1 / (1 + IGV rate) = 0.847 per art. 56 lit. c of the TUO IGV/ISC law (SUNAT guidance); formula in `sunat-einvoicing` §2 and matrix row 33 |
| 4 | FR-07 to FR-10 | Mention XSD, XMLDSig and ZIP | Low | Kept: they are formats required by the problem statement and SUNAT, not a technology choice |
| 5 | Regulatory matrix | Did not show that the XSD was studied nor which error SUNAT rejects each data item with | Medium | Columns "UBL 2.1 XSD" (official cardinality) and "SUNAT error"; §G internal code → SUNAT error |
| 6 | Architecture | "Clean Architecture" (named in the problem statement) was only mentioned | Low | MVC ↔ Hexagonal ↔ Clean equivalence table in `architecture.md` §4b |
| 7 | Regulatory matrix | Did not explain that UBL is generic and that the system uses the Peruvian (SUNAT) customization, not the European one | Medium | §0 "Generic UBL vs. SUNAT UBL (Peru)" with the `PE:SUNAT` attributes |
| 8 | `ports.md`, FR-07 | `UblGeneratorPort` was declared as "UBL 2.1 templates": nothing guaranteed order and cardinality of the XSD | High | **Resolved 25/10:** [ADR 0005](../../adr/0005-ubl-from-xsd-generated-classes.md) — classes generated from the official XSDs + `XmlSerializer`; ports table updated |
| 9 | NFR-05 | Certificate custody had no ADR | Medium | **Resolved 25/10:** [ADR 0006](../../adr/0006-certificate-custody.md) |
| 10 | `common.schema.json` | `UnitOfMeasure` was an open pattern `^[A-Z0-9]{2,3}$` | Medium | **Resolved 25/10:** closed enum of 62 Catalog 03 codes, equivalent to PLE Table 6 (matrix §K) |
| 11 | `invoicing-api.yaml` | No 401/403, no 404 on downloads, no 400 on lists, no 202/502/503 on issuance; 16 lint errors (operations without summary) | Medium | **Resolved 25/10:** reusable responses, summary on every operation; Redocly and Spectral lint with 0 errors and 0 warnings |

Rest of the checklist: all 5 EARS patterns present; 17 FRs with ACs; negative ACs present; measurable NFRs; 8 clarifications; glossary terms; no code folders.
