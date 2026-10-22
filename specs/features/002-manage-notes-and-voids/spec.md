# Spec 002 — Manage notes, voids and daily summary

## Context
An issued Document is immutable; errors, returns and discounts are corrected with Notes or a Voiding notice. Sales receipts are reported to SUNAT through a Daily summary. Both processes have their own deadlines and codes (catalogs 09 and 10).

## Actors
- **Salesperson**, **Administrator**, **SUNAT validator** (defined in the glossary).

## User stories
- US-07: As a Salesperson, I want to issue a credit note for a return or discount to correct a Document without voiding it by hand.
- US-08: As a Salesperson, I want to issue a debit note for interest or a price increase.
- US-09: As an Administrator, I want to void a Document issued by mistake, within the deadline.
- US-10: As an Administrator, I want the day's Sales receipts to be reported automatically in a Daily summary and to see its result.
- US-11: As a Salesperson, I want to check the SUNAT status of any Electronic document and resend pending ones.

## Functional requirements
- **FR-18** (Ubiquitous): The system shall require every Note to state the Electronic document it modifies (type, Series and Sequence number) and a reason from catalog 09 (credit note) or 10 (debit note).
- **FR-19** (Unwanted behaviour): If the Note refers to a Document that is neither ACCEPTED nor OBSERVED, then the system shall block issuance.
- **FR-20** (Ubiquitous): The system shall assign the Note a Series whose first letter matches the modified Document (F for invoice, B for sales receipt).
- **FR-21** (Ubiquitous): The system shall allow each Note to modify a single Document.
- **FR-22** (Unwanted behaviour): If the sum of credit notes on a Document exceeds its total amount, then the system shall block the new note.
- **FR-23** (Event-driven): When the Administrator requests voiding an Electronic document, the system shall include it in a Voiding notice with the reason, send it and query the Ticket until the CDR is obtained.
- **FR-24** (Unwanted behaviour): If voiding is requested later than 7 calendar days counted from the day after issuance, then the system shall reject it and state that a credit note is required.
- **FR-25** (Event-driven): When the issuance day ends, the system shall generate the Daily summary with the unreported Sales receipts and their Notes, in blocks of up to 500 lines, send it and query its Ticket.
- **FR-26** (State-driven): While a Daily summary has no CDR, the system shall query its Ticket periodically and show the status "in progress".
- **FR-27** (Event-driven): When the Salesperson looks up an Electronic document, the system shall show its SUNAT status, the CDR code and description, and allow resending those in PENDING_RESEND.

## Non-functional requirements
- **NFR-08**: 100 % of a day's Sales receipts shall be reported within the following 7 calendar days.
- **NFR-09**: Ticket queries shall be retried at most every 5 minutes, for up to 24 h.

## Acceptance criteria
AC-28 (FR-18) — Note without reason
  Given a credit note without a catalog 09 code
  When the Salesperson tries to issue it
  Then the system blocks it with "Reason is required"

AC-29 (FR-18) — Return per item
  Given the ACCEPTED Invoice F001-00000042 with 10 units of P-001
  When the Salesperson issues a credit note with reason 07 for 3 units
  Then the Note references F001-00000042 and its amount corresponds to 3 units

AC-30 (FR-19) — Note on a rejected document
  Given Document F001-00000043 in REJECTED status
  When the Salesperson tries to issue a credit note on it
  Then the system blocks it

AC-31 (FR-20) — Series of the note
  Given a credit note on Sales receipt B001-00000010
  When the Series is assigned
  Then the Series starts with B

AC-32 (FR-21) — Note on two documents
  Given a credit note referencing F001-00000042 and F001-00000044
  When the Salesperson tries to issue it
  Then the system blocks it with "A note modifies a single document"

AC-33 (FR-22) — Excess credit
  Given F001-00000042 for 118.00 with a previous credit note of 100.00
  When the Salesperson issues another credit note for 20.00
  Then the system blocks it

AC-34 (FR-23) — Voiding accepted
  Given Invoice F001-00000045 issued yesterday
  When the Administrator voids it with reason "Error in description"
  Then a Voiding notice is sent, a Ticket is obtained and on receiving an accepted CDR the Invoice becomes VOIDED

AC-35 (FR-24) — Voiding past the deadline
  Given an Invoice issued 9 calendar days ago
  When the Administrator requests voiding it
  Then the system rejects it stating "Use a credit note"

AC-36 (FR-25) — Summary of 620 sales receipts
  Given 620 Sales receipts issued on 21/10/2026
  When the Daily summary is generated
  Then 2 submissions are created: one of 500 lines and one of 120

AC-37 (FR-26) — Ticket in progress
  Given a Daily summary whose Ticket responds "in progress"
  When the Salesperson opens the list
  Then the Summary shows "in progress" and a query is scheduled

AC-38 (FR-27) — Manual resend
  Given a Document in PENDING_RESEND
  When the Salesperson presses resend and the SUNAT validator responds with code 0
  Then the SUNAT status becomes ACCEPTED

## Out of scope
- Notes for export adjustments (export reason 13).
- Reversals of withholding and perception.

## Clarifications
- [NEEDS CLARIFICATION] Who may issue notes and voids? The Salesperson or only the Administrator?
- [NEEDS CLARIFICATION] Will ABC send Sales receipts via Daily summary or individually? (the regulation allows both)
- [NEEDS CLARIFICATION] Do physical returns re-enter inventory automatically?
- [NEEDS CLARIFICATION] The "one note, one document" rule applies to SEE-OSE from 1/1/2027 (RS 000048-2026); it is adopted now. Does management approve?
