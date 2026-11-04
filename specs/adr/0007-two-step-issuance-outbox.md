---
id: 0007
title: Issue in two transactions with an outbox; CDR handled by code range
status: accepted
origin: judgment (professor feedback on series locking) and problem statement
---
# Context
A sequence number must be consecutive and never reused (constitution 6), so assigning it needs a row lock on the Series (`SeriesRepository.nextSequence`). If the same database transaction also waits for SUNAT or the OSE (`sendBill` can take tens of seconds or time out), every other sale on that Series waits too, and a crash in the middle leaves a number half-used. SUNAT also answers in four different ways (exception, rejection, acceptance, acceptance with observations), and each one must end in a defined state.

# Decision
**Two transactions and a transactional outbox** (FR-50, NFR-13).

| Step | Inside the transaction | Holds the Series lock | Ends in |
|---|---|---|---|
| **T1 — Issue** (use case `IssueDocument`) | Business rules → `nextSequence` → `UblGeneratorPort` → `XsdValidatorPort` → `SignerPort` → store signed XML + hash → insert row in `outbox` | Yes, only here (milliseconds) | `SIGNED` + outbox entry, committed together |
| **T2 — Dispatch** (`OutboxDispatcher`, scheduled job) | Read pending outbox rows (`FOR UPDATE SKIP LOCKED`) → `SunatValidatorPort.sendDocument` → record CDR → mark row done | No | Status by CDR code (table below) |

The API keeps its contract: after T1 commits, the controller waits up to 30 s for T2 of that document. If the CDR arrives, it answers `201` with ACCEPTED / OBSERVED / REJECTED; if not, `202` with PENDING_RESEND (FR-11). A crash between T1 and T2 loses nothing: the outbox row is still there and the signed file is resent unchanged (AC-72).

**CDR handling by return code** (FR-45, [`sunat-return-codes.json`](../catalogs/sunat-return-codes.json)):

| Code | Meaning | New status | Number | Next action |
|---|---|---|---|---|
| `0`, no notes | Accepted | ACCEPTED | Issued | Store CDR, publish `DocumentAccepted` |
| `0` with notes `4000+` | Accepted with observations | OBSERVED | Issued | Store CDR and every observation; alert the Administrator to fix the cause in future documents |
| `0100`–`1999` | Exception: the file was **not processed** (service down, authentication, malformed ZIP) | PENDING_RESEND | Kept | Retry with exponential backoff (1, 2, 4… min, max 1 h) **with the same signed file**; alert 1 day before the deadline (FR-11) |
| `2000`–`3999` | Rejection: the document was processed and is invalid | REJECTED | Consumed, never reused | Show code and description; the Salesperson issues a new document with the next number |
| No answer / timeout | Unknown | PENDING_RESEND | Kept | Before resending, query the status with `getStatusCdr` so the same document is not sent twice |

The same outbox carries the Daily summary (RC) and the Voiding notice (RA), whose answer is a ticket queried with `getStatus` (FR-26).

# Alternatives evaluated
| Option | Lock time | Crash between sign and send | Result |
|---|---|---|---|
| One transaction including `sendBill` | Seconds to 30 s | Rolls back a signed document, or leaves a gap | Rejected |
| **Two transactions + outbox** | Milliseconds | Outbox row survives; resend | **Chosen** |
| Number assigned outside any transaction (sequence object) | None | Gaps in the Series when T1 fails | Rejected: gaps are hard to explain to SUNAT and break the 12.1/14.1 continuity |

# Consequences
+ The Series lock never depends on SUNAT's latency (NFR-13); two tills on different Series never wait for each other (AC-69, AC-71).
+ The outbox is the same mechanism that later publishes domain events to a broker (architecture §5 step 2).
– A document may stay SIGNED for some seconds before being sent; the UI shows "sending".
– The dispatcher must be idempotent: it records the `outboxId` with the CDR and never sends a row already done.
