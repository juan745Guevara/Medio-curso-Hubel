---
id: 0005
title: Generate the UBL XML from classes derived from the official XSDs (no templates)
status: accepted
origin: problem statement
---
# Context
The problem statement asks to study `UBL-Invoice-2.1.xsd` and to comply "strictly" with SUNAT. If the XML is produced from text templates, the XSD is only checked at the end (ADR 0003) and nothing in the code guarantees element order, cardinality or data types: every template change can break the structure. The stack is .NET 10 (ADR 0002).

# Decision
1. **Generated classes.** The `Abc.Ubl` library contains only classes generated from the official OASIS UBL 2.1 XSDs (`maindoc/UBL-Invoice-2.1.xsd`, `UBL-CreditNote-2.1.xsd`, `UBL-DebitNote-2.1.xsd` and `common/*`), versioned in the repository with their hash (ADR 0003). Daily summary (`SummaryDocuments-1.0.xsd`) and voiding (`VoidedDocuments-1.0.xsd`) are generated from SUNAT's own XSDs.
2. **Generator.** [XmlSchemaClassGenerator](https://github.com/mganss/XmlSchemaClassGenerator) (`dotnet-xscgen`), run as a build step, never edited by hand:
   ```
   xscgen -o Abc.Ubl/Generated --nullable \
     -n "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2=Abc.Ubl.Invoice" \
     -n "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2=Abc.Ubl.Cac" \
     -n "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2=Abc.Ubl.Cbc" \
     -n "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2=Abc.Ubl.Ext" \
     xsd/maindoc/UBL-Invoice-2.1.xsd xsd/maindoc/UBL-CreditNote-2.1.xsd xsd/maindoc/UBL-DebitNote-2.1.xsd
   ```
   `xsd.exe` (Windows SDK) is the fallback if the generator is not accepted.
3. **Serialization.** `XmlSerializer` with explicit namespaces (`cac`, `cbc`, `ext`, `ds`), UTF-8 without BOM and no indentation (a signed XML is never reformatted).
4. **SUNAT customization in the mapper, not in the class.** `UblMapper` (Infrastructure) fills the generated classes from `ElectronicDocument` following the [regulatory matrix](../traceability/regulatory-matrix.md): `CustomizationID = 2.0`, catalog codes with `listAgencyName="PE:SUNAT"` and `listURI` (attributes that already exist in the UBL `CodeType`/`IdentifierType`). The `ext:ExtensionContent` (`xsd:any`) is left empty for the `SignerPort`.
5. **Pipeline unchanged.** Mapper → `XmlSerializer` → `XsdValidatorPort` (ADR 0003) → `SignerPort` → `SunatValidatorPort`.

# Alternatives evaluated
| Option | Structure guaranteed by | Result |
|---|---|---|
| **Classes generated from the XSD + XmlSerializer** | The compiler (order, cardinality and types come from the XSD) | **Chosen** |
| Text templates (Razor/Scriban) with SUNAT customization | Only the final XSD validation | Rejected: structural errors are found late and each template duplicates the XSD |
| Building with `XDocument`/`XmlWriter` by hand | Developer discipline | Rejected: same risk as templates, more code |
| Third-party UBL library (e.g. UblSharp) | The library's version of UBL | Rejected: adds a dependency whose UBL version and release pace we do not control |

# Consequences
+ A required element that is missing or out of order is a compilation or serialization error, not a SUNAT rejection.
+ Updating the XSD regenerates the classes; the diff shows exactly what changed.
+ The XSD validation (ADR 0003) remains as a second barrier for values (patterns, lengths).
– The generated classes are large; they are kept in their own project and are not used outside Infrastructure (dependency rule, ADR 0001).
