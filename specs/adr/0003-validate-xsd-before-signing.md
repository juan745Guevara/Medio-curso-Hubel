---
id: 0003
title: Validate against the UBL 2.1 XSD before signing and sending
status: accepted
origin: problem statement
---
# Context
The problem statement asks to study `UBL-Invoice-2.1.xsd` and to comply "strictly" with SUNAT. A malformed XML is rejected by SUNAT/OSE and consumes the sequence number (constitution 6).

# Decision
Fixed pipeline: business rules → generate XML from the XSD-derived classes ([ADR 0005](0005-ubl-from-xsd-generated-classes.md)) → **validate against the local OASIS 2.1 XSD** → sign → send. If the XSD fails, the document returns to DRAFT and is not sent (FR-08). The XSDs are versioned in [`xsd/`](../../xsd/README.md) with a hash manifest that `tools/validate_specs.py` checks on every change.

**The signature slot.** `ext:ExtensionContent` must contain exactly one element, but the `ds:Signature` does not exist yet before signing. The generator therefore puts a neutral placeholder there (`abc:SignaturePlaceholder` in a namespace the XSD does not know, accepted by the `xsd:any` wildcard), the XML is validated, and `SignerPort` replaces the placeholder with the enveloped `ds:Signature`. The signed XML is validated a second time before it is stored. Both steps are proven by `tools/ubl_build.py` on the nine reference files of [`specs/examples/xml/`](../examples/xml/).

# Alternatives evaluated
| Option | Result |
|---|---|
| Rely only on SUNAT/OSE validation | Rejected: late rejections and lost sequence numbers |
| Validate with the SUNAT Facturador rule XSLT in addition to the XSD | Recommended as a future improvement; increases business-rule coverage |

# Consequences
+ No structure error reaches SUNAT.
+ The sequence is executable: the reference XML of the examples go through it and stay valid after signing.
– The XSDs and, in the future, the rules from SUNAT's validation Excel must be maintained.
