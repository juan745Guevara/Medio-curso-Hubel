---
name: ubl-21
description: Cómo obtener, leer y validar UBL 2.1 (UBL-Invoice-2.1.xsd y relacionados de OASIS), armar el XML con la personalización SUNAT, firmarlo con XMLDSig y verificarlo. Úsala para la matriz normativa, el generador UBL y las pruebas de XSD y firma.
---

# UBL 2.1 para SUNAT

## 1. Obtener los XSD oficiales
- Especificación: https://docs.oasis-open.org/ubl/UBL-2.1.html
- Paquete: `os-UBL-2.1.zip` desde https://docs.oasis-open.org/ubl/os-UBL-2.1/
- Archivos necesarios:
  - `xsd/maindoc/UBL-Invoice-2.1.xsd`, `UBL-CreditNote-2.1.xsd`, `UBL-DebitNote-2.1.xsd`
  - `xsd/common/*` (CommonAggregateComponents, CommonBasicComponents, CommonExtensionComponents, xmldsig, UnqualifiedDataTypes…)
- Mantén la estructura de carpetas: `maindoc` importa `../common/`.
- Versiona los XSD dentro del proyecto (ADR 0003) con un archivo `VERSION` y su hash.

## 1b. Personalización peruana, no europea
- El XSD de OASIS es genérico; lo peruano lo pone el Anexo 9-A de SUNAT. Usa `cbc:CustomizationID` = `2.0`, nunca el de EN 16931/Peppol.
- Todo código de catálogo SUNAT lleva `listAgencyName`/`schemeAgencyName="PE:SUNAT"` y `listURI`/`schemeURI` `urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoNN` (tabla en `specs/traceability/matriz-normativa.md` §0).
- Resumen diario y baja usan esquemas propios de SUNAT (`urn:sunat:names:specification:ubl:peru:schema:xsd:…-1`), no los de OASIS.

## 2. Leer el XSD sin perderse
- Raíz: `xsd:element name="Invoice"` → `InvoiceType`, una secuencia ordenada. **El orden de los elementos importa**: un `cbc:IssueDate` después de `cbc:InvoiceTypeCode` invalida el XML.
- `minOccurs="0"` en OASIS no significa opcional para SUNAT: la obligatoriedad real sale del Anexo 9-A (columna "Oblig." de la matriz).
- Orden mínimo para factura SUNAT: `UBLExtensions` → `UBLVersionID` → `CustomizationID` → `ID` → `IssueDate` → `IssueTime` → `DueDate` → `InvoiceTypeCode` → `Note` → `DocumentCurrencyCode` → … → `Signature` → `AccountingSupplierParty` → `AccountingCustomerParty` → … → `PaymentTerms` → `AllowanceCharge` → `TaxTotal` → `LegalMonetaryTotal` → `InvoiceLine`.

## 3. Esqueleto de factura

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
  <!-- cac:Signature, partes, PaymentTerms, TaxTotal, LegalMonetaryTotal, InvoiceLine… -->
</Invoice>
```

## 4. Validar contra el XSD
- .NET (stack del ADR 0002):
  ```csharp
  var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
  set.Add("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", "xsd/maindoc/UBL-Invoice-2.1.xsd");
  var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = set };
  settings.ValidationEventHandler += (_, e) => errores.Add($"{e.Severity}: {e.Message} ({e.Exception?.LineNumber})");
  using var r = XmlReader.Create(xmlStream, settings); while (r.Read()) { }
  ```
- Línea de comandos (revisión rápida): `xmllint --noout --schema xsd/maindoc/UBL-Invoice-2.1.xsd factura.xml`
- Valida **antes** de firmar (ADR 0003). Si falla, el documento vuelve a BORRADOR.

## 5. Firmar (XMLDSig enveloped)
1. Cargar el certificado .pfx con su clave desde el almacén seguro (nunca del repositorio).
2. Firma enveloped con referencia `URI=""`, transform `enveloped-signature`, canonicalización C14N.
3. Insertar `ds:Signature` (con `Id`, p. ej. `SignABC`) dentro de `ext:ExtensionContent`.
4. Incluir `ds:KeyInfo/ds:X509Data/ds:X509Certificate`.
5. El bloque `cac:Signature` del cuerpo debe referenciar el mismo Id.
6. Algoritmo: las implementaciones de referencia peruanas usan RSA-SHA1; confirmar con SUNAT/OSE si aceptan RSA-SHA256 antes de cambiarlo.
7. Verificar la firma después de firmar (`SignedXml.CheckSignature`) y guardar SHA-256 del XML firmado.

## 6. Empaquetar y enviar
- ZIP con un único XML; nombre `RUC-TT-SERIE-NUMERO.zip`.
- `sendBill(fileName, contentFile=base64(zip))` → ZIP con `R-<nombre>.xml` (CDR, `ApplicationResponse`).
- Leer del CDR: `cac:DocumentResponse/cac:Response/cbc:ResponseCode` (0 = aceptado) y `cbc:Description`; observaciones en `cbc:Note`.

## 7. Errores frecuentes
| Síntoma | Causa típica |
|---|---|
| XSD: elemento inesperado | Orden de la secuencia alterado |
| Rechazo por totales | Redondeo por línea distinto del total |
| Firma inválida | XML modificado o reformateado después de firmar (no "pretty-print" tras firmar) |
| Moneda inconsistente | `@currencyID` distinto de `DocumentCurrencyCode` |
