# Official XML schemas (not edited)

These files are the input of [ADR 0005](../specs/adr/0005-ubl-from-xsd-generated-classes.md): the `Abc.Ubl` classes are generated from them, and [ADR 0003](../specs/adr/0003-validate-xsd-before-signing.md) validates every XML against them before signing. **Nobody edits them.** `MANIFEST.sha256` holds the hash of every file (computed with LF line endings, so a Windows checkout gives the same result), and `python tools/validate_specs.py` fails if any file changes.

| Folder | Content | Origin |
|---|---|---|
| `ubl-2.1/maindoc/UBL-Invoice-2.1.xsd` | Invoice, OASIS UBL 2.1 **OS** (04/11/2013) | [docs.oasis-open.org/ubl/os-UBL-2.1](https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd/maindoc/UBL-Invoice-2.1.xsd), file of the problem statement |
| `ubl-2.1/maindoc/UBL-CreditNote-2.1.xsd`, `UBL-DebitNote-2.1.xsd`, `UBL-ApplicationResponse-2.1.xsd` (CDR) | OASIS UBL 2.1 main documents | OASIS UBL 2.1 package as mirrored by [thegreenter/ubl-validator](https://github.com/thegreenter/ubl-validator/tree/master/src/xsd/2.1) (MIT) |
| `ubl-2.1/common/` | The 14 common modules every main document imports (CAC, CBC, EXT, UDT, QDT, signature, xmldsig, XAdES, CCTS) | same mirror |
| `sunat/maindoc/UBL-SummaryDocuments-2.0.xsd`, `UBL-VoidedDocuments-2.0.xsd` | SUNAT's own schemas for the daily summary (RC, `urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1`) and the voiding notice (RA, `…VoidedDocuments-1`) | same mirror ([2.0 folder](https://github.com/thegreenter/ubl-validator/tree/master/src/xsd/2.0)) |
| `sunat/common/` | UBL 2.0 common modules + `UBLPE-SunatAggregateComponents` (`sac:`) that RC and RA import | same mirror |

**Version note.** The mirrored UBL 2.1 files carry the header of the OASIS *Committee Specification 1* (16/06/2013). For `UBL-Invoice-2.1.xsd` the OS file and the mirrored one were compared line by line: they differ **only in the header comment** (library name, generation date and one copyright line); every element, type and cardinality is identical. That is why both can be used together. The OASIS site could not be reached from the build environment; when it can, replace the mirrored files with the OS `xsdrt` ones and regenerate `MANIFEST.sha256` with `python tools/validate_specs.py --write-manifest`.
