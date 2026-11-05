# Regulatory traceability matrix: SUNAT data ↔ UBL 2.1 ↔ DTO

Deliverable 4 of the exam. Blocks A–H cross the UBL document with SUNAT; blocks I–K cross the PLE books (formats 14.1 and 12.1) with their UBL origin. Each row crosses a data item required by SUNAT with its catalog, its cardinality in the official OASIS XSD, its XML tag (Annex 9-A mapping), the DTO field that feeds it, the rule, the error code SUNAT or the OSE rejects with if it fails, and the AC that tests it.

**Scope:** UBL 2.1 in its Peruvian SUNAT customization (see §0).

**Key reading:** the OASIS XSD only makes 6 header elements mandatory (`ID`, `IssueDate`, `AccountingSupplierParty`, `AccountingCustomerParty`, `LegalMonetaryTotal`, `InvoiceLine`). SUNAT makes 22 more data items mandatory that are optional in the XSD (SUNAT column = M with XSD 0..x). That is why validating against the XSD is necessary but not sufficient: the system also applies the Annex 9-A rules and the error catalog ([ADR 0003](../adr/0003-validate-xsd-before-signing.md)).

**Sources**: OASIS [UBL-Invoice-2.1.xsd](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-Invoice-2.1.xsd) and [UBL-CreditNote-2.1.xsd](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-CreditNote-2.1.xsd); aggregate types checked in [data2type UBL 2.1](https://repository.data2type.de/UBL/v_2.1/html/co.TaxSubtotalType.html); Annex 1 and Annex 9-A of RS 097-2012 per [Annex VII RS 114-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexoVII-114-2019.pdf); Annex 8 catalogs ([Annex E RS 245-2017](https://www.sunat.gob.pe/legislacion/superin/2017/anexoE-245-2017.pdf), [Annex D RS 357-2015](https://www.sunat.gob.pe/legislacion/superin/2015/anexoD-357-2015.pdf)); ICBPER [Annexes RS 150-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexos-150-2019.pdf); payment terms [Annex IV RS 193-2020](https://www.sunat.gob.pe/legislacion/superin/2020/anexo4-193-2020.pdf); error codes from SUNAT's return code catalog as maintained by [Greenter (CodeErrors.xml)](https://github.com/thegreenter/xcodes/blob/master/src/data/CodeErrors.xml), to be checked against the current validation Excel on the CPE microsite.

Conventions: `cbc` = CommonBasicComponents-2 · `cac` = CommonAggregateComponents-2 · `ext` = CommonExtensionComponents-2 · `ds` = xmldsig. **SUNAT**: M mandatory, C conditional. **XSD**: element cardinality (and its parents where it matters); in **bold** what the XSD already requires.

## 0. Generic UBL vs. SUNAT UBL (Peru)

OASIS UBL 2.1 (also ISO/IEC 19845) is a **generic, international base**: it defines which tags exist and in which order, but not which data is mandatory in each country, which codes to use or which taxes exist. Each tax authority publishes its own **customization** on top of the same base. This system implements **only SUNAT's Peruvian customization**; it does not use the European one.

| Layer | Europe (another customization) | **Peru — the one used by this system** |
|---|---|---|
| Base | OASIS UBL 2.1 (`UBL-Invoice-2.1.xsd`) | OASIS UBL 2.1 (the **same** `UBL-Invoice-2.1.xsd`) |
| Customizing standard | EN 16931 + Peppol BIS Billing 3.0 | RS 097-2012/SUNAT and amendments, Annex 9-A |
| `cbc:CustomizationID` | `urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0` | **`2.0`** (SUNAT structure version) |
| Taxes | VAT with categories S, Z, E… | **IGV, ISC, ICBPER** (catalog 05) |
| Codes | UN/ECE and CEN code lists | **SUNAT Annex 8 catalogs** (01 to 59), with UN/ECE only for units and currency |
| Party identification | European VAT ID, Peppol ID | **RUC, DNI** (catalog 06) |
| Usual currency | EUR | **PEN** |
| Who validates | Peppol network / each country's authority | **SUNAT or an OSE**, which returns the CDR |
| Own documents (outside OASIS) | — | Daily summary and voiding notice: **SUNAT** schemas (`urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1` and `…VoidedDocuments-1`) built with UBL 2.0 components and the `sac` prefix |

So if an XML carries the EN 16931 `CustomizationID`, PEN does not appear, or taxes are "VAT S", that XML belongs to the European customization and SUNAT rejects it (2072 "CustomizationID - La versión del documento no es la correcta").

**Attributes that mark the XML as Peruvian** (Annex 9-A, [Annex VII RS 114-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexoVII-114-2019.pdf)):

| Tag | SUNAT attributes |
|---|---|
| `cbc:InvoiceTypeCode` (cat. 01) | `@listAgencyName="PE:SUNAT"`, `@listName="Tipo de Documento"`, `@listURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"`, `@listID` = operation type (cat. 51) |
| `cac:PartyIdentification/cbc:ID` (cat. 06) | `@schemeID` (6 = RUC), `@schemeName="Documento de Identidad"`, `@schemeAgencyName="PE:SUNAT"`, `@schemeURI` of catalog 06 |
| `cbc:DocumentCurrencyCode` (cat. 02) | `@listID="ISO 4217 Alpha"`, `@listName="Currency"`, `@listAgencyName="United Nations Economic Commission for Europe"` |
| `cbc:InvoicedQuantity@unitCode` (cat. 03) | `@unitCodeListID="UN/ECE rec 20"`, `@unitCodeListAgencyName="United Nations Economic Commission for Europe"` |
| `cbc:ItemClassificationCode` (cat. 25) | `@listID="UNSPSC"`, `@listAgencyName="GS1 US"`, `@listName="Item Classification"` |

General pattern: every code from a SUNAT-specific catalog carries `listAgencyName`/`schemeAgencyName = "PE:SUNAT"` and a `listURI`/`schemeURI` `urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoNN`. The UBL generator (`UblGeneratorPort`) always adds them. Attribute values stay in Spanish because SUNAT defines them that way.

## A. Header

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Digital signature | RS 097-2012 Annex 9-A | — | M | `UBLExtensions` 0..1 | `ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature` | Generated by `SignerPort` | XMLDSig enveloped; Issuer certificate | 1059, 2335, 2336 | AC-16 |
| 2 | Signature reference | Annex 9-A | — | M | `Signature` 0..n · `ID` 1..1 | `cac:Signature/cbc:ID`, `cac:SignatoryParty`, `cac:DigitalSignatureAttachment` | Issuer configuration | Matches the Id of `ds:Signature` | 2076 | AC-16 |
| 3 | UBL version | Annex 9-A | — | M | 0..1 | `cbc:UBLVersionID` | Constant `2.1` | Equals "2.1" | 2074, 2075 | AC-14 |
| 4 | Structure version | Annex 9-A | — | M | 0..1 | `cbc:CustomizationID` | Constant `2.0` | Equals "2.0" (SUNAT customization) | 2072, 2073 | AC-14 |
| 5 | Series and sequence number | Annex 1 | — | M | **1..1** | `cbc:ID` | `series` + assigned sequence | `^[FB][A-Z0-9]{3}-[0-9]{1,8}$`; unique; consecutive; equal to the file name | 1001, 1002, 1035, 1049 | AC-01, AC-02 |
| 6 | Issue date | Annex 1 | — | M | **1..1** | `cbc:IssueDate` | `issueDate` | yyyy-mm-dd; within the deadline | 1009, 1010, 2329 | AC-15 |
| 7 | Issue time | Annex 9-A | — | C | 0..1 | `cbc:IssueTime` | Server clock | hh:mm:ss | 2806 | — |
| 8 | Document type | Annex 1 | 01 | M | 0..1 | `cbc:InvoiceTypeCode` | `type` | 01 invoice / 03 sales receipt; equal to the file name | 1003, 1004 | AC-14 |
| 9 | Operation type | Annex 1 | 51 | M | optional attribute | `cbc:InvoiceTypeCode/@listID` | `operationType` | 0101 in scope | 3205, 3206 | AC-14 |
| 10 | Amount-in-words legend | Annex 1 | 52 | M | 0..n | `cbc:Note[@languageLocaleID='1000']` | `AmountInWordsConverter(totalAmount)` | Matches the total amount (in Spanish); code not repeated | 3006, 3014 | AC-25 |
| 11 | Currency | Annex 1 | 02 (ISO 4217) | M | 0..1 | `cbc:DocumentCurrencyCode` and `@currencyID` of every amount | `currency` | PEN or USD; same currency throughout | 2069, 2070, 3088, 2071 | — |
| 12 | Due date | Annex 1 | — | C | 0..1 | `cbc:DueDate` | Last installment if Credit | ≥ issue date | — | AC-21 |
| 50 | Exchange rate (USD documents) | RS 108-2020 Annex IV field 27 | 02 | C | `PaymentExchangeRate` 0..1 · `SourceCurrencyCode` 1..1 · `TargetCurrencyCode` 1..1 · `CalculationRate` 0..1 | `cac:PaymentExchangeRate` (`cbc:SourceCurrencyCode`=USD, `cbc:TargetCurrencyCode`=PEN, `cbc:CalculationRate`, `cbc:Date`) | `exchangeRate` | Required if `currency` = USD; > 0; 3 decimals (FR-46) | 2714, 2716, 2719, 2721 | AC-64, AC-65 |

## B. Issuer and Buyer

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 13 | Issuer RUC | Annex 1 | 06 (`6`) | M | `AccountingSupplierParty` **1..1** · `Party` 0..1 · `PartyIdentification` 0..n | `cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID=6` | Issuer configuration | Modulo 11; equal to the RUC in the file name | 1008, 1034, 0154 | AC-03 |
| 14 | Issuer business name | Annex 1 | — | M | `PartyLegalEntity` 0..n · `RegistrationName` 0..1 | `…/cac:Party/cac:PartyLegalEntity/cbc:RegistrationName` | Configuration | Not empty | 1037 | — |
| 15 | Establishment code | Annex 1 | — | M | `RegistrationAddress` 0..1 · `AddressTypeCode` 0..1 | `…/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:AddressTypeCode` | Configuration (0000 = tax address) | 4 digits; declared in the RUC | 3030, 3268 | — |
| 16 | Tax address and ubigeo | Annex 1 | 13 (INEI) | C | `ID` 0..1 · `AddressLine` 0..n | `…/cac:RegistrationAddress/cbc:ID`, `cac:AddressLine/cbc:Line` | Configuration | 6-digit ubigeo | — | — |
| 17 | Buyer document type | Annex 1 | 06 | M (invoice) / C (sales receipt) | `AccountingCustomerParty` **1..1** · `PartyIdentification` 0..n | `cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID` | `buyer.document.type` | Invoice ⇒ 6 | 2015, 2016, 2800, 2017 | AC-05 |
| 18 | Buyer document number | Annex 1 | — | M / C | `PartyIdentification/ID` **1..1** | `…/cac:PartyIdentification/cbc:ID` | `buyer.document.number` | RUC modulo 11; DNI 8 digits | 2014, 2801, 2802 | AC-03, AC-04 |
| 19 | Buyer name | Annex 1 | — | M / C | `RegistrationName` 0..1 | `…/cac:PartyLegalEntity/cbc:RegistrationName` | `buyer.name` | Sales receipt > S/ 700 ⇒ mandatory | 2021, 2022 | AC-06, AC-07 |

## C. Payment terms (RS 193-2020)

In the XML, SUNAT requires the literal values `Contado` (cash), `Credito` (credit) and `Cuota001…` (installments).

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 20 | Cash payment terms | Annex IV RS 193-2020 | — | M (invoice) | `PaymentTerms` 0..n · `ID` 0..1 · `PaymentMeansID` 0..n | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Contado'` | `paymentTerms.type` = CASH | Cash or Credit, not both | 3244, 3245, 3247 | — |
| 21 | Credit and outstanding amount | Annex IV RS 193-2020 | 02 | C | `Amount` 0..1 | `cac:PaymentTerms[cbc:ID='FormaPago']/cbc:PaymentMeansID='Credito'` + `cbc:Amount` | `paymentTerms.outstandingAmount` | Mandatory if Credit; ≤ total amount | 3250, 3251, 3265 | AC-21 |
| 22 | Installments | Annex IV RS 193-2020 | — | C | `PaymentDueDate` 0..1 | `cac:PaymentTerms/cbc:PaymentMeansID='Cuota001'…`, `cbc:Amount`, `cbc:PaymentDueDate` | `paymentTerms.installments[]` | ≥ 1; Σ = outstanding amount; date ≥ issue date | 3246, 3249, 3252, 3253, 3266, 3267 | AC-21, AC-22 |

## D. Item (`/Invoice/cac:InvoiceLine/…`)

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 23 | Line number | Annex 1 | — | M | `InvoiceLine` **1..n** · `ID` **1..1** | `cbc:ID` | item order | 1..n, not repeated | 2023, 2752 | — |
| 24 | Quantity and unit | Annex 1 | 03 (UN/ECE Rec 20) | M | 0..1 | `cbc:InvoicedQuantity` + `@unitCode` | `items[].quantity`, `items[].unit` | Unit in catalog; quantity > 0 | 2024, 2025, 2883, 4320 | AC-08 |
| 25 | Item sale value | Annex 1 | — | M | **1..1** | `cbc:LineExtensionAmount` | Calculated: quantity × unitValue − discount | 2 decimals; matches quantity × value | 2032, 2370, 4288 | AC-08 |
| 26 | Unit price (tax included) | Annex 1 | 16 (`01`) | M | `PricingReference` 0..1 · `AlternativeConditionPrice` 0..n · `PriceTypeCode` 0..1 | `cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount` + `cbc:PriceTypeCode=01` | Calculated | (sale value + taxes) / quantity | 2028, 2029, 2030, 2367, 2409, 2410, 4287 | AC-08 |
| 27 | Unit value (tax excluded) | Annex 1 | — | M | `Price` 0..1 · `PriceAmount` 1..1 | `cac:Price/cbc:PriceAmount` | `items[].unitValue` | ≥ 0 | 2067, 2068 | AC-08 |
| 28 | Description | Annex 1 | — | M | `Item` **1..1** · `Description` 0..n | `cac:Item/cbc:Description` | `items[].description` or Product | ≤ 500 | 2026, 2027 | — |
| 29 | Product code | Annex 1 | — | C | `SellersItemIdentification` 0..1 | `cac:Item/cac:SellersItemIdentification/cbc:ID` | `items[].productCode` | Product classified | 4269 | AC-47 |
| 30 | SUNAT product code | Annex 1 | 25 (UNSPSC) | C | `CommodityClassification` 0..n | `cac:Item/cac:CommodityClassification/cbc:ItemClassificationCode` | Product | Optional in scope; at least third UNSPSC level | 4331, 4337 | — |
| 31 | IGV treatment | Annex 1 | **07** | M | 0..1 | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode` | Product.igvTreatment | 10 taxed; 20 exempt; 30 non-taxable | 2040, 2371 | AC-08, AC-12 |
| 32 | Item IGV | Annex 1 | 05 (`1000`, VAT) | M | `TaxTotal` 0..n · `TaxAmount` 1..1 · `TaxSubtotal` 0..n · `TaxCategory` 1..1 · `Percent` 0..1 · `TaxScheme` 1..1 | `cac:TaxTotal/cac:TaxSubtotal` (`cbc:TaxableAmount`, `cbc:TaxAmount`, `cac:TaxCategory/cbc:Percent=18`, `cac:TaxScheme/cbc:ID=1000`) | Calculated | IGV = 18 % × (sale value + ISC); a single treatment tax per line | 2033, 2034, 2035, 2036, 2037, 3103, 3105, 3106, 3111, 4290 | AC-08, AC-09 |
| 33 | **Item ISC system** | Annex 1 | **08** | C | `TierRange` 0..1 | `cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TierRange` | Product.isc.system | 01 ad valorem (sale value × rate) / 02 fixed amount (units × amount) / 03 retail sale price (suggested price × 0.847 × rate, art. 56 lit. c); only if the tax is ISC | 2041, 2373, 3210 | AC-09, AC-10 |
| 34 | Item ISC amount | Annex 1 | 05 (`2000`, EXC) | C | `TaxSubtotal` 0..n | `cac:TaxSubtotal` with `cac:TaxScheme/cbc:ID=2000`, `cbc:TaxableAmount`, `cbc:TaxAmount`, `cbc:Percent` | Calculated | System 01: rate × sale value; 02: amount × quantity; 03: per Appendix IV lit. C | 3104, 3108 | AC-09, AC-10 |
| 35 | Item ICBPER | [Annexes RS 150-2019](https://www.sunat.gob.pe/legislacion/superin/2019/anexos-150-2019.pdf) | 05 (`7152`, OTH) | C | `BaseUnitMeasure` 0..1 · `PerUnitAmount` 0..1 | `cac:TaxSubtotal/cbc:BaseUnitMeasure` (bags) and `cac:TaxSubtotal/cac:TaxCategory/cbc:PerUnitAmount` (rate), `cac:TaxScheme/cbc:ID=7152` | Product.isPlasticBag, quantity | S/ 0.50 × bags; outside the IGV base | 2949, 3236, 3237, 3238 | AC-11 |
| 36 | Exempt / non-taxable item | Annex 1 | 05 (`9997` EXO, `9998` INA) | C | `TaxSubtotal` 0..n | `cac:TaxSubtotal` with `cac:TaxScheme/cbc:ID=9997` or `9998`, `cbc:TaxAmount=0` | Calculated | TaxAmount 0 | 3101, 3110 | AC-12 |
| 51 | Free-of-charge item: reference value | Annex 1 | **16** (`02`) | C | `PricingReference` 0..1 · `AlternativeConditionPrice` 0..n | `cac:PricingReference/cac:AlternativeConditionPrice` with `cbc:PriceTypeCode=02` and `cbc:PriceAmount` = reference unit value; `cac:Price/cbc:PriceAmount` = 0.00 | `items[].referenceUnitValue`, `items[].unitValue`=0.00 | Treatments 11–16, 21, 31–37 only; reference > 0 (FR-47) | 2425, 2640, 2410 | AC-66, AC-67 |
| 52 | Free-of-charge item: tax | Annex 1 | 05 (`9996` GRA, FRE) · 07 | C | `TaxSubtotal` 0..n | `cac:TaxSubtotal` with `cac:TaxScheme/cbc:ID=9996`, `cbc:TaxableAmount` = quantity × reference value, `cbc:TaxAmount` = IGV of the reference value for 11–16, 0.00 for 21 and 31–37, `cbc:TaxExemptionReasonCode` = the treatment | Calculated | Not part of `LineExtensionAmount` totals nor `PayableAmount` | 3101, 3110, 2641 | AC-67 |

## E. Totals

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 37 | Total IGV | Annex 1 | 05 | M | `TaxTotal` 0..n · `TaxAmount` **1..1** | `/Invoice/cac:TaxTotal/cbc:TaxAmount` and `cac:TaxSubtotal[TaxScheme/ID=1000]` | `totals.igv` | Σ item IGV = total taxes | 2048, 2049, 2638, 4301 | AC-13 |
| 38 | Total taxed sale value | Annex 1 | 05 | C | `TaxableAmount` 0..1 | `cac:TaxSubtotal[ID=1000]/cbc:TaxableAmount` | `totals.taxed` | Σ items 10–17 | 3010, 3011, 4294 | AC-13 |
| 39 | Total exempt / non-taxable | Annex 1 | 05 | C | `TaxableAmount` 0..1 | `cac:TaxSubtotal[ID=9997 / 9998]/cbc:TaxableAmount` | `totals.exempt`, `totals.nonTaxable` | Σ items 20–21 / 30–37 | 4296, 4297 | AC-13 |
| 40 | Total ISC | Annex 1 | 05 | C | `TaxAmount` 1..1 | `cac:TaxSubtotal[ID=2000]/cbc:TaxAmount` | `totals.isc` | Σ ISC | 2638, 4301 | AC-09 |
| 41 | Total ICBPER | RS 150-2019 | 05 | C | `TaxAmount` 1..1 | `cac:TaxSubtotal[ID=7152]/cbc:TaxAmount` | `totals.icbper` | Σ ICBPER | 4321 | AC-11 |
| 42 | Total sale value | Annex 1 | — | M | `LegalMonetaryTotal` **1..1** · `LineExtensionAmount` 0..1 | `cac:LegalMonetaryTotal/cbc:LineExtensionAmount` | Σ sale value | 2 decimals | 2487, 2488, 4309 | AC-13 |
| 43 | Total sale price | Annex 1 | — | M | `TaxInclusiveAmount` 0..1 | `cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount` | Σ sale value + taxes | Equal to calculated | 2592, 3019, 4211 | AC-13 |
| 44 | Global discounts | Annex 1 | 53 | C | `AllowanceTotalAmount` 0..1 | `cac:AllowanceCharge` + `cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount` | `totals.discounts` | Pending clarification | 2065, 2419 | — |
| 45 | Total amount | Annex 1 | — | M | `PayableAmount` **1..1** | `cac:LegalMonetaryTotal/cbc:PayableAmount` | `totals.totalAmount` | = 43 − 44; rounding within the allowed limit | 2043, 2044, 4314 | AC-13 |
| 53 | Total free-of-charge value | Annex 1 | 05 (`9996`) | C | `TaxSubtotal` 0..n | `/Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID=9996]/cbc:TaxableAmount` | Σ free lines | > 0 when there is a free line; not added to `PayableAmount` | 2641, 3057 | AC-67 |
| 54 | Free-of-charge legend | Annex 1 | 52 (`1002`) | C | `Note` 0..n | `cbc:Note[@languageLocaleID='1002']` = "TRANSFERENCIA GRATUITA DE UN BIEN Y/O SERVICIO PRESTADO GRATUITAMENTE" | Generated | Mandatory when row 53 > 0 | 2416 | AC-67 |

## F. Notes (CreditNote / DebitNote)

In `UBL-CreditNote-2.1.xsd`: `ID`, `IssueDate`, `AccountingSupplierParty`, `AccountingCustomerParty`, `LegalMonetaryTotal` are 1..1 and `CreditNoteLine` 1..n; `DiscrepancyResponse` and `BillingReference` are 0..n even though SUNAT requires them.

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 46 | Credit note type | Annex 3 | **09** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/CreditNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `reason.code` | 01–10, 13 | 2127, 2128 | AC-28 |
| 47 | Debit note type | Annex 4 | **10** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/DebitNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `reason.code` | 01–03 | 2172, 2173 | AC-28 |
| 48 | Justification | Annex 3/4 | — | M | `Description` 0..n | `cac:DiscrepancyResponse/cbc:Description` | `reason.description` | Not empty | 2135, 2136 | AC-28 |
| 49 | Modified document | Annex 3/4 | 01 | M | `BillingReference` 0..n · `InvoiceDocumentReference` 0..1 · `ID` 1..1 · `DocumentTypeCode` 0..1 | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` and `cbc:DocumentTypeCode` | `modifiedDocument` | Only one; exists; not voided or rejected | 2116, 2117, 2119, 2120, 2121, 2412 | AC-30, AC-32 |
| 55 | Debit note total | Annex 4 | — | M | `RequestedMonetaryTotal` **1..1** · `PayableAmount` **1..1** | `/DebitNote/cac:RequestedMonetaryTotal/cbc:PayableAmount` (a DebitNote has no `LegalMonetaryTotal`) | `totals.totalAmount` | Same rules as row 45 | 2062, 2149 | AC-75 |
| 56 | Note line quantity | Annex 3/4 | 03 | M | `CreditNoteLine` **1..n** · `CreditedQuantity` 0..1 / `DebitNoteLine` **1..n** · `DebitedQuantity` 0..1 | `/CreditNote/cac:CreditNoteLine/cbc:CreditedQuantity/@unitCode`, `/DebitNote/cac:DebitNoteLine/cbc:DebitedQuantity/@unitCode` (never `InvoiceLine` inside a note) | `items[].quantity`, `items[].unit` | Unit in catalog 03 | 2138, 2139, 2188, 2189 | AC-29 |

A Note is its own UBL root document (`CreditNote`, `DebitNote`), a sibling of `Invoice`, not a subclass of it; in the domain it is the same `ElectronicDocument` aggregate with type 07/08 and a `NoteReference`.

## F2. Daily summary (RC) — SUNAT schema `SummaryDocuments-1`

Not an OASIS document: SUNAT's own XSD ([`xsd/sunat/maindoc/UBL-SummaryDocuments-2.0.xsd`](../../xsd/sunat/maindoc/UBL-SummaryDocuments-2.0.xsd)) built with UBL **2.0** components and the `sac:` prefix (`urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1`). Reference file: [`examples/xml/20131312955-RC-20261020-1.xml`](../examples/xml/20131312955-RC-20261020-1.xml).

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | SUNAT XSD (min..max) | Tag | Source | Rule | SUNAT error | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 57 | Versions | RS 097-2012 † | — | M | `UBLVersionID` 0..1 · `CustomizationID` **1..1** | `cbc:UBLVersionID`=2.0, `cbc:CustomizationID`=1.1 | Constants | Exact values | 2212, 2214 | AC-36 |
| 58 | Summary identifier | RS 097-2012 † | — | M | `ID` **1..1** | `cbc:ID` = `RC-YYYYMMDD-n` | `DailySummary.id` | Date = generation date; n correlative per day; equal to the file name | 2210, 2346 | AC-36 |
| 59 | Date of the documents / of generation | RS 097-2012 † | — | M | `ReferenceDate` **1..1** · `IssueDate` **1..1** | `cbc:ReferenceDate`, `cbc:IssueDate` | `referenceDate`, clock | ReferenceDate ≤ IssueDate ≤ today | 2233, 2234, 2237 | AC-36 |
| 60 | Issuer | RS 097-2012 † | 06 | M | `AccountingSupplierParty` **1..1** | `cac:AccountingSupplierParty/cbc:CustomerAssignedAccountID` (RUC), `cbc:AdditionalAccountID`=6, `cac:Party/cac:PartyLegalEntity/cbc:RegistrationName` | Issuer | Same RUC as the file name | 1034 | — |
| 61 | Line, document and number | RS 097-2012 † | 01 | M | `SummaryDocumentsLine` **1..n** · `LineID` **1..1** · `DocumentTypeCode` **1..1** · `ID` 0..1 | `sac:SummaryDocumentsLine/cbc:LineID`, `cbc:DocumentTypeCode` (03, 07, 08), `cbc:ID` (B001-10) | `SummaryLine` | ≤ 500 lines per summary; LineID 1..n | 2238, 2239, 2240 | AC-36 |
| 62 | Buyer | RS 097-2012 † | 06 | C | `AccountingCustomerParty` 0..1 | `cac:AccountingCustomerParty/cbc:CustomerAssignedAccountID`, `cbc:AdditionalAccountID` | Buyer | Mandatory when the receipt > S/ 700 | 2014 | AC-06 |
| 63 | Modified document (notes) | RS 097-2012 † | 01 | C | `BillingReference` 0..1 | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID`, `cbc:DocumentTypeCode` | `NoteReference` | Only for lines 07/08 | 2116 | AC-30 |
| 64 | Line status | RS 097-2012 † | **19** | M | `Status` 0..1 | `cac:Status/cbc:ConditionCode` (1 add, 2 modify, 3 void) | `SummaryLine.conditionCode` | 3 voids a receipt already reported (FR-23 for receipts) | 2280 | AC-34 |
| 65 | Total and amounts by treatment | RS 097-2012 † | **11** | M | `TotalAmount` **1..1** · `BillingPayment` 0..n | `sac:TotalAmount`, `sac:BillingPayment/cbc:PaidAmount` + `cbc:InstructionID` (01 taxed, 02 exempt, 03 non-taxable, 05 free) | Totals | Same as the receipt | 2251, 2252, 2256, 2257 | AC-36 |
| 66 | Taxes of the line | RS 097-2012 † | 05 | M | `TaxTotal` 0..n | `cac:TaxTotal/cbc:TaxAmount` + `cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme` (1000, 2000, 7152) | Totals | Same as the receipt | 2048 | AC-36 |

## F3. Voiding notice (RA) — SUNAT schema `VoidedDocuments-1`

XSD [`xsd/sunat/maindoc/UBL-VoidedDocuments-2.0.xsd`](../../xsd/sunat/maindoc/UBL-VoidedDocuments-2.0.xsd); reference file [`examples/xml/20131312955-RA-20261021-1.xml`](../examples/xml/20131312955-RA-20261021-1.xml).

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | SUNAT XSD (min..max) | Tag | Source | Rule | SUNAT error | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 67 | Versions and identifier | RS 097-2012 † | — | M | `CustomizationID` **1..1** · `ID` **1..1** | `cbc:UBLVersionID`=2.0, `cbc:CustomizationID`=1.0, `cbc:ID` = `RA-YYYYMMDD-n` | `VoidingNotice.id` | Equal to the file name | 2283 | AC-76 |
| 68 | Date of the voided documents / of the notice | RS 097-2012 † | — | M | `ReferenceDate` **1..1** · `IssueDate` **1..1** | `cbc:ReferenceDate`, `cbc:IssueDate` | `referenceDate`, clock | IssueDate − ReferenceDate ≤ 7 calendar days (FR-24) | 2302, 2303, 2374 | AC-35 |
| 69 | Voided document | RS 097-2012 † | 01 | M | `VoidedDocumentsLine` **1..n** · `DocumentTypeCode`, `DocumentSerialID`, `DocumentNumberID` **1..1** | `sac:VoidedDocumentsLine/cbc:LineID`, `cbc:DocumentTypeCode`, `sac:DocumentSerialID`, `sac:DocumentNumberID` | `VoidedLine` | Document ACCEPTED/OBSERVED and not already voided | 2105, 2106, 2244, 2323 | AC-34 |
| 70 | Reason | RS 097-2012 † | — | M | `VoidReasonDescription` **1..1** | `sac:VoidReasonDescription` | `reason` | Not empty, ≤ 100 | 2314, 2315 | AC-34 |

† Annex of RS 097-2012 (as amended) for the daily summary and the voiding notice; the annex number is to be verified. The structure, order and cardinality of every tag above were checked against SUNAT's own XSD, and the reference files validate against it.

## G. From SUNAT error to API error

Each rule is validated **before** sending, with the API's internal code (`error.schema.json`, field `code`). If SUNAT/OSE still rejects, its code is returned in `sunatCode` (FR-10, AC-18).

| Internal code (API) | Local validation | SUNAT errors it prevents | AC |
|---|---|---|---|
| `INVALID_RUC` | Modulo 11 | 2802, 1034 | AC-04 |
| `INVOICE_REQUIRES_RUC` | Invoice ⇒ type 6 | 2016, 2017, 2800 | AC-05 |
| `RECEIPT_REQUIRES_DOCUMENT` | Sales receipt > S/ 700 ⇒ document and name | 2014, 2015, 2021 | AC-06 |
| `CREDIT_WITHOUT_INSTALLMENTS` | Credit ⇒ ≥ 1 installment and outstanding amount | 3249, 3251 | AC-21 |
| `INSTALLMENTS_MISMATCH` | Σ installments = outstanding ≤ total | 3265, 3266 | AC-22 |
| `INVALID_XSD` | OASIS 2.1 XSD | 1002, 1010, 2075, 2073, 2063 | AC-15 |
| `INCONSISTENT_TOTALS` | Recalculate lines and totals | 4287, 4288, 4290, 4301, 4309, 4211, 4321 | AC-08 to AC-13 |
| `INVALID_ISC_SYSTEM` | Catalog 08 only with tax 2000 | 2041, 2373, 3210 | AC-09, AC-10 |
| `MULTIPLE_DOCUMENT_NOTE` | One note, one document | 2412 | AC-32 |
| `DOCUMENT_NOT_ACCEPTED` | Modified document ACCEPTED/OBSERVED | 2119, 2120, 2121 | AC-30 |
| `VOIDING_PAST_DEADLINE` | 7 calendar days | 2374 | AC-35 |
| `SUBMISSION_PAST_DEADLINE` (alert) | Submission deadline | 2108, 2600 | AC-20 |
| `SUNAT_RULES` | SUNAT rules on the XML before signing (`SunatRulesValidator`, FR-52); 2000–3999 stop, 4000+ are kept as observations | 1001, 2071, 2072, 2074, 2076, 2416, 2641, 2716, 2721, 2752, 2800, 2802, 2024, 3014, 3205, 3206, 3244, 3249, 3251, 3265, 3266, 3267, 4211, 4288, 4290, 4301, 4309, 4312 | AC-77, AC-78 |
| `INVALID_SIGNATURE` | Verify signature after signing; do not reformat | 1059, 2335, 2336 | AC-16 |

## G2. CDR return code ranges → SUNAT status (FR-45, ADR 0007)

The 1,710 codes are versioned in [`catalogs/sunat-return-codes.json`](../catalogs/sunat-return-codes.json) with their class; `tools/validate_specs.py` checks that every code in this matrix exists there.

| Code in the answer | Class | Example | SUNAT status | Sequence number | AC |
|---|---|---|---|---|---|
| `ResponseCode` = 0, no notes | Accepted | — | ACCEPTED | Issued | AC-17 |
| `ResponseCode` = 0 with `cbc:Note` coded 4000+ | Observation | 4287 unit price differs | OBSERVED (valid document) | Issued | AC-63 |
| SOAP fault 0100–1999 | Exception: not processed | 0109 authentication service down; 0130 no ticket | PENDING_RESEND; resend the same signed file | Kept | AC-61 |
| `ResponseCode` 2000–3999 | Rejection | 2800 buyer document type | REJECTED | Consumed, never reused | AC-62, AC-18 |
| No answer in 30 s | Unknown | timeout | PENDING_RESEND; query `getStatusCdr` before resending | Kept | AC-19 |

## H. Cross-reference with feature and port

| Block | Feature | Output port that generates the tag |
|---|---|---|
| A, B, C, D, E | 001 | `UblGeneratorPort` (data), `SignerPort` (rows 1–2) |
| F, F2, F3 | 002 | `UblGeneratorPort` (CreditNote/DebitNote classes from OASIS XSD; RC/RA classes from SUNAT XSD) |
| G2 | 001, 002 | `SunatValidatorPort` + `ResponseCodeClassifier` |
| Sending the `RUC-TT-SERIES-NUM.zip` file | 001, 002 | `SunatValidatorPort` |
| I, J, K | 004 | `GenerateSalesRegister`, `GenerateInventoryRegister` (read views over `DocumentRepository` and `MovementRepository`) |

## I. PLE format 14.1 (Registro de Ventas e Ingresos) ← UBL

Structure of [RS 108-2020/SUNAT Annex IV](https://www.sunat.gob.pe/legislacion/superin/2020/anexos-108-2020.pdf). DTO: [`sales-register-row.schema.json`](../contracts/schemas/sales-register-row.schema.json). The TXT writes the 35 fields in this order separated by `|` (FR-41).

| Field | Data | PLE table | UBL origin (or internal) | DTO | Rule | AC |
|---|---|---|---|---|---|---|
| 1 | Period | — | `cbc:IssueDate` → AAAAMM00 | `period` | Month of the entry; later Periods use status 8 | AC-53 |
| 2 | CUO | — | Internal: `ElectronicDocument.id` (UUID) | `cuo` | ≤ 40 characters | AC-53 |
| 3 | Entry number | — | Internal: `M` + monthly sequence | `entryNumber` | Starts with A, M or C | AC-53 |
| 4 | Issue date | — | `cbc:IssueDate` | `issueDate` | DD/MM/AAAA in the TXT | AC-53 |
| 5 | Due or payment date | — | `cbc:DueDate` or last `cac:PaymentTerms/cbc:PaymentDueDate` | `dueDate` | Mandatory only for type 14 | — |
| 6 | Document type | 10 | `cbc:InvoiceTypeCode`; 07 for `CreditNote`, 08 for `DebitNote` | `documentType` | Same codes as Catalog 01 | AC-53, AC-54 |
| 7 | Series | — | `cbc:ID` before the hyphen | `series` | F/B + 3 | AC-53 |
| 8 | Number | — | `cbc:ID` after the hyphen | `number` | 1–8 digits | AC-53 |
| 9 | Final number | — | Not used (individual entries) | `finalNumber` | Empty | — |
| 10 | Customer document type | 2 | `cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID/@schemeID` | `customerDocumentType` | Same codes as Catalog 06 | AC-53 |
| 11 | Customer document number | — | `…/cac:PartyIdentification/cbc:ID` | `customerDocumentNumber` | ≤ 15 | AC-53 |
| 12 | Customer name | — | `…/cac:PartyLegalEntity/cbc:RegistrationName` | `customerName` | ≤ 100 | AC-53 |
| 13 | Export value | — | `cac:TaxSubtotal[TaxScheme/ID=9995]/cbc:TaxableAmount` | `exportValue` | 0.00 in scope | — |
| 14 | Taxed base | — | `cac:TaxSubtotal[TaxScheme/ID=1000]/cbc:TaxableAmount` | `taxedBase` | Without ISC | AC-53 |
| 15 | Base discount | — | `cac:AllowanceCharge[ChargeIndicator=false][AllowanceChargeReasonCode=02]/cbc:Amount` | `taxedBaseDiscount` | Negative or 0.00 | — |
| 16 | IGV + IPM | — | `cac:TaxSubtotal[TaxScheme/ID=1000]/cbc:TaxAmount` | `igv` | 18 % (15.5 + 2.5) | AC-53 |
| 17 | IGV discount | — | Internal: IGV of field 15 | `igvDiscount` | — | — |
| 18 | Exempt | — | `cac:TaxSubtotal[TaxScheme/ID=9997]/cbc:TaxableAmount` | `exempt` | — | AC-48 |
| 19 | Non-taxable | — | `cac:TaxSubtotal[TaxScheme/ID=9998]/cbc:TaxableAmount` | `nonTaxable` | — | AC-48 |
| 20 | ISC | — | `cac:TaxSubtotal[TaxScheme/ID=2000]/cbc:TaxAmount` | `isc` | Catalog 08 system (matrix row 33) | AC-48 |
| 21 | IVAP base | — | `cac:TaxSubtotal[TaxScheme/ID=1016]/cbc:TaxableAmount` | `ivapBase` | 0.00 in scope | — |
| 22 | IVAP | — | `cac:TaxSubtotal[TaxScheme/ID=1016]/cbc:TaxAmount` | `ivap` | 0.00 in scope | — |
| 23 | ICBPER | — | `cac:TaxSubtotal[TaxScheme/ID=7152]/cbc:TaxAmount` | `icbper` | S/ 0.50 per bag | AC-48 |
| 24 | Other charges and taxes | — | `cac:TaxSubtotal[TaxScheme/ID=9999]/cbc:TaxAmount` + `cac:LegalMonetaryTotal/cbc:ChargeTotalAmount` | `otherCharges` | Outside the base | — |
| 25 | Total | — | `cac:LegalMonetaryTotal/cbc:PayableAmount` | `totalAmount` | Negative for credit notes | AC-49, AC-53, AC-54 |
| 26 | Currency | 4 | `cbc:DocumentCurrencyCode` | `currency` | Same codes as Catalog 02 | AC-55 |
| 27 | Exchange rate | — | Internal: SUNAT exchange rate of `cbc:IssueDate` | `exchangeRate` | #.###; 1.000 for PEN | AC-53, AC-55 |
| 28 | Modified document date | — | Internal: issue date of the document in `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` | `modifiedDocument.issueDate` | Types 07/08 | AC-54 |
| 29 | Modified document type | 10 | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:DocumentTypeCode` | `modifiedDocument.documentType` | Types 07/08 | AC-54 |
| 30 | Modified document series | — | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` before the hyphen | `modifiedDocument.series` | Types 07/08 | AC-54 |
| 31 | Modified document number | — | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` after the hyphen | `modifiedDocument.number` | Types 07/08 | AC-54 |
| 32 | Contract or project | — | Not used | `contractId` | Consortia only | — |
| 33 | Exchange-rate error | — | Internal | `exchangeRateError` | '1' or empty | — |
| 34 | Paid with payment means | 1 | Internal (payment record) | `paidWithPaymentMeans` | '1' or empty | — |
| 35 | Status | — | Internal: `SunatStatus` + Period (FR-44) | `entryStatus` | ACCEPTED/OBSERVED → 1; VOIDED → 2 with 0.00; late → 8; REJECTED/PENDING_RESEND not exported | AC-50, AC-60 |

## J. PLE format 12.1 (Inventario Permanente en Unidades Físicas) ← UBL and legacy

Structure of [RS 042-2018/SUNAT Annex 2](https://www.sunat.gob.pe/legislacion/superin/2018/anexo-042-2018.pdf). DTO: [`stock-card.schema.json`](../contracts/schemas/stock-card.schema.json). The TXT writes fields 1–19 (FR-42); the final balance is only shown on the stock card (FR-40).

| Field | Data | PLE table | UBL origin (or internal / legacy) | DTO | Rule | AC |
|---|---|---|---|---|---|---|
| 1 | Period | — | Movement date → AAAAMM00 | `period` | — | AC-56 |
| 2 | CUO | — | Internal: `InventoryMovement.id` (UUID) | `cuo` | ≤ 40 | AC-56 |
| 3 | Entry number | — | Internal: `A` opening balance, `M` + sequence | `entryNumber` | A/M/C | AC-56 |
| 4 | Establishment code | — | `InventoryMovement.establishment` (= `cac:AccountingSupplierParty/cac:Party/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:AddressTypeCode` of the document) | `establishmentCode` | 4 digits; 9999 third party | AC-56 |
| 5 | Catalog used | 13 | Fixed `9` (own codes) | `catalogCode` | — | AC-58 |
| 6 | Stock type | 5 | `Product.existenceType` | `existenceType` | Mandatory (FR-43) | AC-56, AC-59 |
| 7 | Own code | — | `cac:InvoiceLine/cac:Item/cac:SellersItemIdentification/cbc:ID` = `Product.code` | `productCode` | ≤ 24 | AC-56 |
| 8 | Standard catalog | 13 | Fixed `1` (UNSPSC) when field 9 exists | `standardCatalogCode` | Not 9 | — |
| 9 | UNSPSC / GTIN code | — | `cac:Item/cac:CommodityClassification/cbc:ItemClassificationCode` (padded to 16 digits) | `standardProductCode` | When the document carries it | — |
| 10 | Document date | — | `cbc:IssueDate` of the document; legacy `fecha` | `documentDate` | DD/MM/AAAA | AC-56, AC-58 |
| 11 | Document type | 10 | `cbc:InvoiceTypeCode` (01/03), 07/08 for notes; legacy `00` | `documentType` | Not 00 for operations 01–06 | AC-56 |
| 12 | Document series | — | `cbc:ID` before the hyphen; legacy `0` | `documentSeries` | — | AC-56 |
| 13 | Document number | — | `cbc:ID` after the hyphen; legacy `nro_documento` | `documentNumber` | ≤ 20 | AC-56 |
| 14 | Operation type | 12 | Internal: 01 sale (invoice/receipt), 24 customer return (credit note reasons 06/07), 16 opening balance, 99 legacy | `operationType` | — | AC-56 |
| 15 | Description | — | `cac:InvoiceLine/cac:Item/cbc:Description` = `Product.description` | `productDescription` | ≤ 80 | AC-56 |
| 16 | Unit | 6 | `cac:InvoiceLine/cbc:InvoicedQuantity/@unitCode` (Catalog 03) → Table 6 (§K) | `unitOfMeasure` | Must have Table 6 code (FR-43) | AC-56, AC-59 |
| 17 | Inflows | — | `InventoryMovement.quantity` when type INFLOW; credit note `cac:CreditNoteLine/cbc:CreditedQuantity` | `inflow` | Positive or 0.00 | AC-56 |
| 18 | Outflows | — | `cac:InvoiceLine/cbc:InvoicedQuantity`; legacy `salida` | `outflow` | Negative or 0.00 | AC-56 |
| — | Final balance (printed format) | — | Internal: previous balance + inflow + outflow | `finalBalance` | Not in the TXT | AC-51, AC-56 |
| 19 | Status | — | Internal: 1 Period, 8/9 previous Period | `status` | — | AC-58 |

## K. Catalog 03 (UBL `@unitCode`) ↔ PLE Table 6

`UnitOfMeasure` is a closed enum of the Catalog 03 codes that also exist in Table 6 ([RS 169-2015 Annex 3](https://www.sunat.gob.pe/legislacion/superin/2015/anexo3-rs169-2015.pdf)), so the equivalence is 1:1 for every accepted value and the 12.1 export never needs a conversion. A unit outside this list is rejected by the contract (400) before reaching the domain.

| Catalog 03 (UBL) | Table 6 (PLE 12.1) | Unit |
|---|---|---|
| `4A` | `4A` | Bobbins |
| `BJ` | `BJ` | Bucket |
| `BLL` | `BLL` | Barrels |
| `BG` | `BG` | Bag |
| `BO` | `BO` | Bottles |
| `BX` | `BX` | Box |
| `CT` | `CT` | Cartons |
| `CMK` | `CMK` | Square centimetre |
| `CMQ` | `CMQ` | Cubic centimetre |
| `CMT` | `CMT` | Centimetre |
| `CEN` | `CEN` | Hundred units |
| `CY` | `CY` | Cylinder |
| `CJ` | `CJ` | Cones |
| `DZN` | `DZN` | Dozen |
| `DZP` | `DZP` | Dozen pack |
| `BE` | `BE` | Bundle |
| `GLI` | `GLI` | Imperial gallon |
| `GRM` | `GRM` | Gram |
| `GRO` | `GRO` | Gross |
| `HLT` | `HLT` | Hectolitre |
| `LEF` | `LEF` | Leaf |
| `SET` | `SET` | Set |
| `KGM` | `KGM` | Kilogram |
| `KTM` | `KTM` | Kilometre |
| `KWH` | `KWH` | Kilowatt hour |
| `KT` | `KT` | Kit |
| `CA` | `CA` | Cans |
| `LBR` | `LBR` | Pounds |
| `LTR` | `LTR` | Litre |
| `MWH` | `MWH` | Megawatt hour |
| `MTR` | `MTR` | Metre |
| `MTK` | `MTK` | Square metre |
| `MTQ` | `MTQ` | Cubic metre |
| `MGM` | `MGM` | Milligrams |
| `MLT` | `MLT` | Millilitre |
| `MMT` | `MMT` | Millimetre |
| `MMK` | `MMK` | Square millimetre |
| `MMQ` | `MMQ` | Cubic millimetre |
| `MLL` | `MLL` | Thousands |
| `UM` | `UM` | Million units |
| `ONZ` | `ONZ` | Ounces |
| `PF` | `PF` | Pallets |
| `PK` | `PK` | Package |
| `PR` | `PR` | Pair |
| `FOT` | `FOT` | Feet |
| `FTK` | `FTK` | Square feet |
| `FTQ` | `FTQ` | Cubic feet |
| `C62` | `C62` | Pieces |
| `PG` | `PG` | Plates |
| `ST` | `ST` | Sheet |
| `INH` | `INH` | Inches |
| `RM` | `RM` | Ream |
| `DR` | `DR` | Drum |
| `STN` | `STN` | Short ton |
| `LTN` | `LTN` | Long ton |
| `TNE` | `TNE` | Tonnes |
| `TU` | `TU` | Tubes |
| `NIU` | `NIU` | Unit (goods) |
| `ZZ` | `ZZ` | Unit (services) |
| `GLL` | `GLL` | US gallon |
| `YRD` | `YRD` | Yard |
| `YDK` | `YDK` | Square yard |

## Gaps in the matrix
- Every SUNAT code cited here exists in the versioned return-code list (checked by `tools/validate_specs.py`); the official validation Excel of the CPE microsite could not be opened from the build environment, so the *rule text* of each code is the one in that list.
- Maximum decimal precision of unit values and rounding tolerance of totals (error 4314): confirm in the current validation Excel.
- Error codes are those of the Invoice group; sales receipts and notes have equivalents with other numbers (e.g. 2110/2111 instead of 2074/2075).
