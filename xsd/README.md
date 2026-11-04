# Official XML schemas (not edited)

These files are the input of [ADR 0005](../specs/adr/0005-ubl-from-xsd-generated-classes.md): the `Abc.Ubl` classes are generated from them, and [ADR 0003](../specs/adr/0003-validate-xsd-before-signing.md) validates every XML against them before signing. **Nobody edits them.** `MANIFEST.sha256` holds the hash of every file (computed with LF line endings, so a Windows checkout gives the same result), and `python tools/validate_specs.py` fails if any file changes.

| Folder | Content | Origin |
|---|---|---|
| `ubl-2.1/maindoc/` | `UBL-Invoice-2.1.xsd` (file of the problem statement), `UBL-CreditNote-2.1.xsd`, `UBL-DebitNote-2.1.xsd`, `UBL-ApplicationResponse-2.1.xsd` (CDR) | **OASIS UBL 2.1 OS** (04/11/2013), folder `xsd/maindoc` of the official package [UBL-2.1.zip](http://docs.oasis-open.org/ubl/os-UBL-2.1/UBL-2.1.zip) |
| `ubl-2.1/common/` | The 14 common modules every main document imports (CAC, CBC, EXT, UDT, QDT, signature, xmldsig, XAdES, CCTS) | same official package, folder `xsd/common` |
| `sunat/maindoc/UBL-SummaryDocuments-2.0.xsd`, `UBL-VoidedDocuments-2.0.xsd` | SUNAT's own schemas for the daily summary (RC, `urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1`) and the voiding notice (RA, `…VoidedDocuments-1`) | SUNAT schemas as mirrored by [thegreenter/ubl-validator](https://github.com/thegreenter/ubl-validator/tree/master/src/xsd/2.0) (MIT); SUNAT does not publish them inside the OASIS package |
| `sunat/common/` | UBL 2.0 common modules + `UBLPE-SunatAggregateComponents` (`sac:`) that RC and RA import | same mirror |

**Version note.** Every UBL 2.1 file comes from the official OASIS 2.1 OS package (the documented `xsd/` variant, the same as the file given in the problem statement; `xsdrt/` is the same schema without annotations). Before this replacement two of them (CreditNote, DebitNote) came from a mirror carrying the Committee Specification 1 header; they were compared with the OS files and differed only in that header comment. If the package is updated, replace the files and run `python tools/validate_specs.py --write-manifest`.
