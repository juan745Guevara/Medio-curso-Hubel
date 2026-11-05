---
id: 0009
title: Amazonía IGV exemption (Law 27037) as configuration of the establishment, not as code
status: accepted
origin: judgment (relevant for a company in the jungle region)
---
# Context
The exam is set at UNAS, Tingo María (province of Leoncio Prado, Huánuco). Law 27037 art. 3 includes Leoncio Prado, Puerto Inca, Marañón and Pachitea in the Amazonía, and art. 13 exempts from IGV the **sale of goods made in the zone for consumption in the zone** by taxpayers located there that meet art. 11.2 (head office and registration in the zone, assets/activities ≥ 70 % in the zone) ([FAOLEX copy of Law 27037](https://faolex.fao.org/docs/pdf/per21100.pdf); current wording to be verified against El Peruano). The problem statement does not say where ABC is. If it is in the zone, most grocery sales are exempt (catalog 07 code **20**, tax **9997**) and the ISC still applies; if a hard-coded "taxed 10" is assumed, every document would be wrong.

# Decision
1. **The exemption is data, not code.** The establishment (`EstablishmentCode`) has an attribute `amazoniaLaw27037` (false by default) with its effective date; the Product keeps its `igvTreatment`. `IssuancePolicy` resolves the Item treatment: if the establishment is under the law and the Product is a good consumed in the zone, the Item goes as `20` (exempt) unless the Product is excluded by the law's list.
2. **Nothing else changes:** `TaxCalculator` already handles code 20 (AC-12), the matrix row 36 maps it to `9997`, and the PLE 14.1 reports it in field 16 (exempt).
3. **Not activated** until management answers the clarification below; the scope of the exam stays "domestic sales, taxed or exempt as the Product says".

# Alternatives evaluated
| Option | Result |
|---|---|
| Ignore the law | Rejected: wrong IGV for a company in the zone |
| Hard-code "exempt" in the jungle build | Rejected: breaks sales to other zones and branches outside it |
| **Establishment attribute + treatment resolved by `IssuancePolicy`** | **Chosen** |

# Consequences
+ A branch inside and another outside the zone issue correctly from the same system.
– The Administrator must keep the attribute and the Products' exclusions up to date.

Resolved ([D-21](../clarifications.md)): not applied in this version; `amazoniaLaw27037` stays false for every establishment until ABC's accountant certifies art. 11.2. The configuration of this ADR is kept so that turning it on needs no code change.
