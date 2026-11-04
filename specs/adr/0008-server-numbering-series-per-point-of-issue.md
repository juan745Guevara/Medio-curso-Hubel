---
id: 0008
title: Numbering and signing only on the server; one Series per point of issue; offline queue without numbers
status: accepted
origin: judgment (professor feedback on multi-device numbering) and problem statement
---
# Context
The app runs natively on Windows (MSI) and Android (APK) (NFR-04), possibly on several tills at the same time and sometimes without connection. If each device numbered and signed with its own local database, two devices using F001 would repeat numbers, and the Digital certificate would have to be copied to every phone, where it can be extracted (ADR 0006).

# Decision
1. **The server is the only one that numbers and signs** (FR-49). The clients (MAUI) never hold a Series counter or the certificate. `SeriesRepository.nextSequence` runs in the server's T1 transaction ([ADR 0007](0007-two-step-issuance-outbox.md)).
2. **One Series per document type and point of issue.** The Administrator registers each point of issue (establishment code + till) and assigns it its Series: for example till 1 of establishment 0000 → F001 / B001, till 2 → F002 / B002 (FR-51). A user logged in at a till can only issue in that till's Series. Notes use the Series of the till that issues them, with the letter of the modified document (FR-20).
3. **Offline: queue requests, not documents.** Without connection the app stores the sale request in its local SQLite with an `Idempotency-Key` and shows it as "pending to issue", with no number. When connection returns it sends the request; if it arrives twice, the server returns the same Document (AC-70). The legal issue date is the date the server issues it.
4. **When the server cannot reach SUNAT**, the Document is still issued, numbered and signed, and stays PENDING_RESEND (FR-11). Only a failure of the whole system (server down) justifies paper Contingency (FR-16).

# Alternatives evaluated
| Option | Duplicate numbers | Certificate exposure | Offline sales | Result |
|---|---|---|---|---|
| Each device numbers and signs (local SQLite + .pfx) | Yes, unless every device has its own Series and never shares it | High: .pfx on every phone | Yes | Rejected |
| Ranges of numbers reserved per device | No | High (signing still local) | Yes | Rejected: gaps when a device is lost; certificate still distributed |
| **Server numbers and signs; Series per point of issue; offline request queue** | No | None on devices | Request is captured; issued on reconnection | **Chosen** |

# Consequences
+ No duplicate numbers by construction; the 14.1 register has no gaps.
+ The certificate exists only in the server (ADR 0006).
– A sale captured offline is not a legal document until the server issues it; the Salesperson can hand over a provisional ticket marked "not a payment voucher" and the Printed representation afterwards. [NEEDS CLARIFICATION] Does management accept this for stores with poor connectivity?
