# Regulatory traceability matrix: SUNAT data ↔ UBL 2.1 ↔ DTO

Deliverable 4 of the exam. Each row crosses a data item required by SUNAT with its catalog, its cardinality in the official OASIS XSD, its XML tag (Annex 9-A mapping), the DTO field that feeds it, the rule, the error code SUNAT or the OSE rejects with if it fails, and the AC that tests it.

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

## F. Notes (CreditNote / DebitNote)

In `UBL-CreditNote-2.1.xsd`: `ID`, `IssueDate`, `AccountingSupplierParty`, `AccountingCustomerParty`, `LegalMonetaryTotal` are 1..1 and `CreditNoteLine` 1..n; `DiscrepancyResponse` and `BillingReference` are 0..n even though SUNAT requires them.

| # | SUNAT data | Regulation / annex | Catalog | SUNAT | UBL 2.1 XSD (min..max) | UBL 2.1 tag | DTO field / source | Rule / validation | SUNAT error if it fails | AC |
|---|---|---|---|---|---|---|---|---|---|---|
| 46 | Credit note type | Annex 3 | **09** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/CreditNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `reason.code` | 01–10, 13 | 2127, 2128 | AC-28 |
| 47 | Debit note type | Annex 4 | **10** | M | `DiscrepancyResponse` 0..n · `ResponseCode` 0..1 | `/DebitNote/cac:DiscrepancyResponse/cbc:ResponseCode` | `reason.code` | 01–03 | 2172, 2173 | AC-28 |
| 48 | Justification | Annex 3/4 | — | M | `Description` 0..n | `cac:DiscrepancyResponse/cbc:Description` | `reason.description` | Not empty | 2135, 2136 | AC-28 |
| 49 | Modified document | Annex 3/4 | 01 | M | `BillingReference` 0..n · `InvoiceDocumentReference` 0..1 · `ID` 1..1 · `DocumentTypeCode` 0..1 | `cac:BillingReference/cac:InvoiceDocumentReference/cbc:ID` and `cbc:DocumentTypeCode` | `modifiedDocument` | Only one; exists; not voided or rejected | 2116, 2117, 2119, 2120, 2121, 2412 | AC-30, AC-32 |

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
| `INVALID_SIGNATURE` | Verify signature after signing; do not reformat | 1059, 2335, 2336 | AC-16 |

## H. Cross-reference with feature and port

| Block | Feature | Output port that generates the tag |
|---|---|---|
| A, B, C, D, E | 001 | `UblGeneratorPort` (data), `SignerPort` (rows 1–2) |
| F | 002 | `UblGeneratorPort` |
| Sending the `RUC-TT-SERIES-NUM.zip` file | 001, 002 | `SunatValidatorPort` |

## Gaps in the matrix
- Maximum decimal precision of unit values and rounding tolerance of totals (error 4314): confirm in the current validation Excel.
- Error codes are those of the Invoice group; sales receipts and notes have equivalents with other numbers (e.g. 2110/2111 instead of 2074/2075).
