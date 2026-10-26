---
name: ubl-21
description: How to obtain, read and validate UBL 2.1 (OASIS UBL-Invoice-2.1.xsd and related), build the XML with SUNAT's Peruvian customization, sign it with XMLDSig and verify it. Use it for the regulatory matrix, the UBL mapper over XSD-generated classes (ADR 0005) and XSD/signature tests.
---

# UBL 2.1 for SUNAT

## 1. Get the official XSDs
- Specification: https://docs.oasis-open.org/ubl/UBL-2.1.html
- Package: `UBL-2.1.zip` from https://docs.oasis-open.org/ubl/os-UBL-2.1/
- Files needed:
  - `xsd/maindoc/UBL-Invoice-2.1.xsd`, `UBL-CreditNote-2.1.xsd`, `UBL-DebitNote-2.1.xsd`
  - `xsd/common/*` (CommonAggregateComponents, CommonBasicComponents, CommonExtensionComponents, xmldsig, UnqualifiedDataTypes…)
- Keep the folder structure: `maindoc` imports `../common/`.
- Version the XSDs inside the project (ADR 0003) with a `VERSION` file and its hash.

## 1b. Peruvian customization, not European
- The OASIS XSD is generic; SUNAT's Annex 9-A adds the Peruvian layer. Use `cbc:CustomizationID` = `2.0`, never the EN 16931/Peppol one.
- Every SUNAT catalog code carries `listAgencyName`/`schemeAgencyName="PE:SUNAT"` and `listURI`/`schemeURI` `urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoNN` (table in `specs/traceability/regulatory-matrix.md` §0).
- Daily summary and voiding use SUNAT's own schemas (`urn:sunat:names:specification:ubl:peru:schema:xsd:…-1`), not OASIS ones.

## 2. Reading the XSD without getting lost
- Root: `xsd:element name="Invoice"` → `InvoiceType`, an ordered sequence. **Element order matters**: a `cbc:IssueDate` after `cbc:InvoiceTypeCode` invalidates the XML.
- `minOccurs="0"` in OASIS does not mean optional for SUNAT: the real mandatory flag comes from Annex 9-A (column "SUNAT" of the matrix).
- Minimum order for a SUNAT invoice: `UBLExtensions` → `UBLVersionID` → `CustomizationID` → `ID` → `IssueDate` → `IssueTime` → `DueDate` → `InvoiceTypeCode` → `Note` → `DocumentCurrencyCode` → … → `Signature` → `AccountingSupplierParty` → `AccountingCustomerParty` → … → `PaymentTerms` → `AllowanceCharge` → `TaxTotal` → `LegalMonetaryTotal` → `InvoiceLine`.

## 3. Generate the XML from XSD-derived classes (ADR 0005)
Never write the XML with templates or string concatenation.
1. Generate the classes from the versioned XSDs with XmlSchemaClassGenerator (command in [ADR 0005](../../../specs/adr/0005-ubl-from-xsd-generated-classes.md)); `xsd.exe` is the fallback.
2. `UblMapper` fills the generated objects from `ElectronicDocument`, row by row of the regulatory matrix:
   ```csharp
   // Illustrative: exact class/property names depend on the generator's naming option
   var invoice = new InvoiceType {
       UblVersionId = new UblVersionIdType { Value = "2.1" },
       CustomizationId = new CustomizationIdType { Value = "2.0" },
       Id = new IdType { Value = doc.Number.ToString() },
       InvoiceTypeCode = new InvoiceTypeCodeType {
           Value = "01", ListId = "0101", ListAgencyName = "PE:SUNAT",
           ListName = "Tipo de Documento", ListUri = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01" },
       // ... parties, TaxTotal, LegalMonetaryTotal, InvoiceLine
   };
   ```
3. Serialize with `XmlSerializer` and the `cac`/`cbc`/`ext`/`ds` prefixes in an `XmlSerializerNamespaces`; `XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false }`.
4. The `ext:ExtensionContent` stays empty: the `SignerPort` inserts `ds:Signature` there afterwards.
5. Validate the result against the same XSD (§4) before signing.

Expected result (excerpt):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
  xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
  xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
  xmlns:ext="urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2"
  xmlns:ds="http://www.w3.org/2000/09/xmldsig#">
  <ext:UBLExtensions><ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension></ext:UBLExtensions>
  <cbc:UBLVersionID>2.1</cbc:UBLVersionID>
  <cbc:CustomizationID>2.0</cbc:CustomizationID>
  <cbc:ID>F001-00000042</cbc:ID>
  <cbc:IssueDate>2026-10-22</cbc:IssueDate>
  <cbc:InvoiceTypeCode listID="0101" listAgencyName="PE:SUNAT" listName="Tipo de Documento"
    listURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01">01</cbc:InvoiceTypeCode>
  <cbc:Note languageLocaleID="1000">SETENTA Y NUEVE CON 00/100 SOLES</cbc:Note>
  <cbc:DocumentCurrencyCode>PEN</cbc:DocumentCurrencyCode>
  <!-- cac:Signature, parties, PaymentTerms, TaxTotal, LegalMonetaryTotal, InvoiceLine… -->
</Invoice>
```

## 4. Validate against the XSD
- .NET (ADR 0002 stack):
  ```csharp
  var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
  set.Add("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", "xsd/maindoc/UBL-Invoice-2.1.xsd");
  var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = set };
  settings.ValidationEventHandler += (_, e) => errors.Add($"{e.Severity}: {e.Message} ({e.Exception?.LineNumber})");
  using var r = XmlReader.Create(xmlStream, settings); while (r.Read()) { }
  ```
- Command line (quick check): `xmllint --noout --schema xsd/maindoc/UBL-Invoice-2.1.xsd invoice.xml`
- Validate **before** signing (ADR 0003). If it fails, the document returns to DRAFT.

## 5. Sign (XMLDSig enveloped)
1. Load the .pfx certificate and its password from the secure store (never from the repository).
2. Enveloped signature with reference `URI=""`, `enveloped-signature` transform, C14N canonicalization.
3. Insert `ds:Signature` (with an `Id`, e.g. `SignABC`) inside `ext:ExtensionContent`.
4. Include `ds:KeyInfo/ds:X509Data/ds:X509Certificate`.
5. The `cac:Signature` block in the body must reference the same Id.
6. Algorithm: Peruvian reference implementations use RSA-SHA1; confirm with SUNAT/OSE whether RSA-SHA256 is accepted before changing it.
7. Verify the signature after signing (`SignedXml.CheckSignature`) and store the SHA-256 of the signed XML.

## 6. Package and send
- ZIP with a single XML; name `RUC-TT-SERIES-NUMBER.zip`.
- `sendBill(fileName, contentFile=base64(zip))` → ZIP with `R-<name>.xml` (CDR, `ApplicationResponse`).
- Read from the CDR: `cac:DocumentResponse/cac:Response/cbc:ResponseCode` (0 = accepted) and `cbc:Description`; observations in `cbc:Note`.

## 7. Common errors
| Symptom | Typical cause |
|---|---|
| XSD: unexpected element | XML written by hand or with a template instead of the generated classes (ADR 0005) |
| Rejection due to totals | Line rounding differs from the total |
| Invalid signature | XML modified or reformatted after signing (no pretty-print after signing) |
| Inconsistent currency | `@currencyID` differs from `DocumentCurrencyCode` |
| CustomizationID rejected (2072) | European (EN 16931/Peppol) example used instead of SUNAT's `2.0` |
