---
id: 0003
title: Validate against the UBL 2.1 XSD before signing and sending
status: accepted
origin: problem statement
---
# Context
The problem statement asks to study `UBL-Invoice-2.1.xsd` and to comply "strictly" with SUNAT. A malformed XML is rejected by SUNAT/OSE and consumes the sequence number (constitution 6).

# Decision
Fixed pipeline: business rules → generate XML → **validate against the local OASIS 2.1 XSD** → sign → send. If the XSD fails, the document returns to DRAFT and is not sent (FR-08). The XSDs are versioned with the system and downloaded from the official OASIS package.

# Alternatives evaluated
| Option | Result |
|---|---|
| Rely only on SUNAT/OSE validation | Rejected: late rejections and lost sequence numbers |
| Validate with the SUNAT Facturador rule XSLT in addition to the XSD | Recommended as a future improvement; increases business-rule coverage |

# Consequences
+ No structure error reaches SUNAT.
– The XSDs and, in the future, the rules from SUNAT's validation Excel must be maintained.
