# Spec 001 — Issue electronic document

## Context
ABC Software Corp sells groceries wholesale and retail and today cannot issue valid documents: it does not validate RUC, does not calculate IGV, ISC or ICBPER, does not generate UBL XML, does not sign and does not communicate with SUNAT or an OSE. RS 097-2012/SUNAT and its amendments require it to issue electronic Invoices (*Factura*) and Sales receipts (*Boleta*) as signed, validated UBL 2.1 XML.

## Actors
- **Salesperson**: records the sale and issues the Document.
- **Administrator**: configures Issuer, Series, Digital certificate, Products and SUNAT validator.
- **SUNAT validator**: SUNAT or an OSE; receives the UBL XML and returns the CDR.
- **Buyer**: receives the Printed representation and the XML.

## User stories
- US-01: As a Salesperson, I want to issue an Invoice to a customer with a RUC so they can claim tax credit.
- US-02: As a Salesperson, I want to issue a Sales receipt to a final consumer to meet the obligation to deliver a document.
- US-03: As a Salesperson, I want the system to calculate IGV, ISC and ICBPER per Item so the totals are never wrong.
- US-04: As a Salesperson, I want to know immediately whether SUNAT or the OSE accepted or rejected the Document and why, so I can correct it in time.
- US-05: As a Salesperson, I want to give the customer the PDF with QR code and the XML so they can verify it.
- US-06: As an Administrator, I want to configure Series, Digital certificate and SUNAT validator without touching code, to operate in test and production.

## Functional requirements
- **FR-01** (Ubiquitous): The system shall assign each Document a unique Series and Sequence number, consecutive per Series, written in `cbc:ID` and in the file name as `SSSS-N` (4-character Series, hyphen, Sequence number of 1 to 8 digits without leading zeros, 1 to 99,999,999), where an Invoice Series starts with F and a Sales receipt Series with B.
- **FR-02** (Event-driven): When the Salesperson enters an Identity document of type 6, the system shall validate that the RUC has 11 digits, prefix 10, 15, 16, 17 or 20 and a correct modulo-11 check digit before allowing issuance.
- **FR-03** (Unwanted behaviour): If the Salesperson tries to issue an Invoice to a Buyer without a valid RUC, then the system shall block issuance and state that an Invoice requires a RUC.
- **FR-04** (State-driven): While the total amount of a Sales receipt exceeds S/ 700.00, the system shall require the Buyer's Identity document type, number and name.
- **FR-05** (Ubiquitous): The system shall calculate per Item the sale value, the ISC according to the Product's ISC system (catalog 08), the IGV on sale value plus ISC for taxed Items (catalog 07 code 10), and the ICBPER per bag outside the IGV base.
- **FR-06** (Ubiquitous): The system shall calculate the Document totals: total per IGV treatment (taxed, exempt, non-taxable), total IGV, total ISC, total ICBPER, total discounts and total amount, rounded to 2 decimals.
- **FR-07** (Event-driven): When the Salesperson confirms issuance, the system shall generate the UBL 2.1 XML with `UBLVersionID` 2.1 and `CustomizationID` 2.0, the catalog 51 Operation type and the mandatory data of Annex 9-A.
- **FR-08** (Unwanted behaviour): If the UBL XML does not validate against the OASIS XSD, then the system shall stop the process, neither sign nor send, and log the structure error.
- **FR-09** (Ubiquitous): The system shall sign the UBL XML with the Issuer's Digital certificate using XMLDSig enveloped inside `ext:UBLExtensions` before sending it.
- **FR-10** (Event-driven): When a Document becomes SIGNED, the system shall send it to the configured SUNAT validator (SUNAT or OSE) in a ZIP named `RUC-TT-SERIES-NUMBER`, and record the CDR with its code and description.
- **FR-11** (Unwanted behaviour): If the SUNAT validator does not respond or there is a network failure, then the system shall leave the Document in PENDING_RESEND, retry automatically and alert when 1 day remains before the submission deadline; before each retry it shall ask SUNAT for the Document's CDR (`getStatusCdr`) and resend the same signed file only if SUNAT has no CDR for it ([ADR 0007](../../adr/0007-two-step-issuance-outbox.md)).
- **FR-12** (Ubiquitous): The system shall record the Invoice Payment terms: Cash, or Credit with Outstanding amount and each Installment with amount and due date.
- **FR-13** (Event-driven): When the Document is ACCEPTED or OBSERVED, the system shall generate the Printed representation with a QR code and allow downloading it together with the XML and the CDR.
- **FR-14** (Optional feature): Where the Buyer has a registered e-mail address, the system shall send them the Printed representation and the XML once ACCEPTED.
- **FR-15** (Ubiquitous): The system shall include in the UBL XML the legend with the total amount in words (catalog 52, code 1000), written in Spanish as SUNAT requires.
- **FR-16** (Optional feature): Where the Administrator has activated Contingency due to a documented failure, the system shall allow recording paper contingency Documents and generate their report for SUNAT within 7 calendar days.
- **FR-17** (Ubiquitous): The system shall deduct from inventory the quantity of each Item of an ACCEPTED or OBSERVED Document as an outflow Inventory movement linked to the Document.
- **FR-45** (Event-driven): When the SUNAT validator returns a response code, the system shall set the SUNAT status by its range ([`sunat-return-codes.json`](../../catalogs/sunat-return-codes.json)): `0` without observations → ACCEPTED; `0100`–`1999` (exception: the file was not processed) → PENDING_RESEND, resending **the same signed file with the same number** after the cause is fixed or the service recovers; `2000`–`3999` → REJECTED, the number stays consumed and is never reused; `0` with observation notes coded `4000` or more → OBSERVED (accepted with observations), recording every observation code and description.
- **FR-46** (State-driven): While the Document currency is USD, the system shall require the exchange rate of the issue date (SUNAT's published *tipo de cambio venta*, 3 decimals), include it in `cac:PaymentExchangeRate` and use it for the PLE 14.1 field 27.
- **FR-47** (State-driven): While an Item has a free-of-charge IGV treatment (catalog 07 codes 11–16, 21, 31–37), the system shall require Unit value 0.00 and a Reference unit value greater than 0 (catalog 16 code 02), report it under tax `9996` (catalog 05) with the IGV calculated on the reference value for codes 11–16 (reported, not charged; 0.00 for 21 and 31–37), leave it out of the total amount and add the legend `1002` (catalog 52).
- **FR-48** (Ubiquitous): The system shall calculate the fixed-amount ISC (catalog 08 system 02) on the quantity expressed in the unit the tax is set in: quantity × Product `iscUnitsPerItem` (for example 0.62 litres per 620 ml bottle) × fixed amount.
- **FR-49** (Ubiquitous): The system shall assign Series and Sequence numbers and sign only on the server. Each point of issue (till, establishment) has its own Series per document type, assigned by the Administrator; a client app never numbers, signs or stores the Digital certificate, and its offline queue only holds unsent requests with their `Idempotency-Key`.
- **FR-50** (Ubiquitous): The system shall issue in two transactions: (1) validate, number, generate the XML, validate the XSD, sign, store as SIGNED and write the submission to the outbox, committing in one database transaction that holds the Series row lock only while it runs; (2) a dispatcher sends the outbox entries to the SUNAT validator outside that transaction and records the CDR.
- **FR-51** (Event-driven): When the Administrator creates a Series, the system shall validate the pattern by document type (`^F[A-Z0-9]{3}$` for 01, `^B[A-Z0-9]{3}$` for 03, notes with the letter of the documents they modify), its establishment and point of issue, start it at 0 and never allow its last Sequence number to be lowered.
- **FR-52** (Unwanted behaviour): If the UBL XML that passed the XSD breaks one of SUNAT's validation rules cited in the regulatory matrix (§G, internal code `SUNAT_RULES`) with a code from `2000` to `3999`, then the system shall stop before signing, neither sign nor send, and return every broken rule with SUNAT's code and text; a rule with a code `4000` or more shall not stop the process and shall be recorded with the Document as an observation.

## Non-functional requirements
- **NFR-01**: Issuing an Invoice of up to 50 Items, from confirmation to signed XML, shall take ≤ 2 s at the 95th percentile.
- **NFR-02**: 100 % of the XML sent shall have passed XSD validation (verifiable in the audit log).
- **NFR-03**: Invoices shall be sent to the SUNAT validator within the current deadline (up to 3 calendar days counted from the day after issuance); the system shall report 0 overdue Invoices without a prior alert.
- **NFR-04**: The application shall run on Windows 10 or later installed via MSI and on Android 10 or later installed via APK.
- **NFR-05**: The Digital certificate and its password shall be stored encrypted and never appear in logs or responses.
- **NFR-06**: XML, CDR and Printed representation shall be kept for at least 5 years without modification (verifiable hash).
- **NFR-07**: Switching between SUNAT and OSE, or between test (beta) and production, shall be a configuration change without recompiling.
- **NFR-13**: The Series row lock shall last only the first issuance transaction (≤ 300 ms at the 95th percentile); the SUNAT validator's response time shall never extend it ([ADR 0007](../../adr/0007-two-step-issuance-outbox.md)).

## Acceptance criteria
AC-01 (FR-01) — Consecutive sequence per Series
  Given the last Invoice in Series F001 is F001-41
  When the Salesperson issues a new Invoice in F001
  Then the Document receives F001-42

AC-02 (FR-01) — Sequence number is never reused
  Given F001-42 was REJECTED by the SUNAT validator
  When the Salesperson issues another Invoice in F001
  Then it receives F001-43 and F001-42 stays recorded as REJECTED

AC-03 (FR-02) — Valid RUC
  Given RUC 20131312955
  When the Salesperson enters it as Identity document type 6
  Then the system accepts it as valid

AC-04 (FR-02) — RUC with wrong check digit
  Given RUC 20131312954
  When the Salesperson enters it
  Then the system rejects it with "Invalid RUC: check digit"

AC-05 (FR-03) — Invoice without RUC
  Given a Buyer identified with DNI 45678912
  When the Salesperson tries to issue an Invoice
  Then the system blocks issuance with "An invoice requires the buyer's RUC"

AC-06 (FR-04) — Sales receipt over S/ 700 without document
  Given a Sales receipt with total amount S/ 767.00 and no Identity document
  When the Salesperson tries to issue it
  Then the system requires document type, number and name

AC-07 (FR-04) — Sales receipt under S/ 700 without document
  Given a Sales receipt with total amount S/ 85.50 and no Identity document
  When the Salesperson issues it
  Then the Document is issued with a generic Buyer

AC-08 (FR-05) — IGV of a taxed Item
  Given a taxed Item (catalog 07 = 10) of 10 units with Unit value S/ 5.00 and no ISC
  When the system calculates
  Then sale value = 50.00, IGV = 9.00 and Item amount = 59.00

AC-09 (FR-05) — Ad valorem ISC before IGV
  Given a taxed Item with sale value S/ 100.00, ISC system 01 and an ISC rate of 25 % configured on the Product
  When the system calculates
  Then ISC = 25.00, IGV base = 125.00, IGV = 22.50 and Item amount = 147.50

AC-10 (FR-05) — Fixed-amount ISC
  Given an Item of 12 units with ISC system 02 and a configured fixed amount of S/ 1.25 per unit
  When the system calculates
  Then ISC = 15.00

AC-11 (FR-05) — ICBPER outside the IGV base
  Given a "plastic bag" Item of 3 units with Unit value S/ 0.10 and ICBPER configured at S/ 0.50 per bag
  When the system calculates
  Then ICBPER = 1.50, IGV = 0.05 (18 % of 0.30, not of 1.80) and Item amount = 1.85

AC-12 (FR-05) — Exempt Item
  Given an Item with IGV treatment 20
  When the system calculates
  Then IGV = 0.00 and the Item adds to the exempt total

AC-13 (FR-06) — Document totals
  Given the Items of AC-08 and AC-12 (exempt with sale value 20.00)
  When the system calculates totals
  Then taxed total = 50.00, exempt total = 20.00, IGV total = 9.00 and total amount = 79.00

AC-14 (FR-07) — UBL header
  Given a confirmed Invoice
  When the UBL XML is generated
  Then it contains UBLVersionID = 2.1, CustomizationID = 2.0, InvoiceTypeCode = 01 with listID = 0101

AC-15 (FR-08) — Invalid XML is not sent
  Given a UBL XML without the mandatory IssueDate element
  When it is validated against the XSD
  Then the process stops, the SUNAT status stays DRAFT and no submission is recorded

AC-16 (FR-09) — Signature present
  Given a valid UBL XML
  When the system signs it
  Then ds:Signature exists inside ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent and the signature verifies with the Issuer's certificate

AC-17 (FR-10) — CDR accepted
  Given a SIGNED Document
  When the SUNAT validator responds with a CDR with code 0
  Then the SUNAT status becomes ACCEPTED and the CDR is stored

AC-18 (FR-10) — CDR rejected
  Given a SIGNED Document with a business error
  When the SUNAT validator responds with a rejection code
  Then the SUNAT status becomes REJECTED and the returned code and description are shown

AC-19 (FR-11) — No connection
  Given the SUNAT validator does not respond within 30 s
  When the system tries to send
  Then the SUNAT status becomes PENDING_RESEND and a retry is scheduled

AC-20 (FR-11) — Deadline alert
  Given an Invoice in PENDING_RESEND issued 2 calendar days ago
  When the deadline check runs
  Then an expiry alert is raised for the Administrator

AC-79 (FR-11) — Lost answer, SUNAT already has the CDR
  Given F001-42 is in PENDING_RESEND because the answer to sendBill was lost
  When the retry runs and getStatusCdr returns a CDR with code 0
  Then F001-42 becomes ACCEPTED with that CDR and it is not sent again

AC-85 (FR-11) — SUNAT status on demand
  Given F001-42 is in PENDING_RESEND
  When the Administrator asks for its SUNAT status
  Then the system queries getStatusCdr, returns the status SUNAT reports and stores the CDR if SUNAT has one, without sending the document

AC-80 (FR-11) — Lost answer, SUNAT has no CDR
  Given F001-42 is in PENDING_RESEND and getStatusCdr returns no CDR
  When the retry runs
  Then the same signed file 20131312955-01-F001-42.zip is sent again with the same number and its CDR is recorded

AC-21 (FR-12) — Credit without installments
  Given an Invoice with Credit payment terms and no Installments
  When the Salesperson tries to issue it
  Then the system blocks it with "A credit invoice requires installments"

AC-22 (FR-12) — Installments do not add up
  Given a credit Invoice with Outstanding amount 1000.00 and Installments adding up to 900.00
  When the Salesperson tries to issue it
  Then the system blocks it with "Installments differ from the outstanding amount"

AC-23 (FR-13) — Printed representation
  Given an ACCEPTED Document
  When the Salesperson asks to download it
  Then they get the PDF with QR, the signed XML and the CDR

AC-24 (FR-14) — E-mail delivery
  Given a Buyer with a registered e-mail address and an ACCEPTED Document
  When the system processes the acceptance
  Then it sends the PDF and XML to that address

AC-25 (FR-15) — Amount in words legend
  Given an Invoice with total amount 79.00 PEN
  When the UBL XML is generated
  Then cbc:Note with languageLocaleID = 1000 contains "SETENTA Y NUEVE CON 00/100 SOLES"

AC-26 (FR-16) — Contingency past the deadline
  Given a contingency Document issued 8 calendar days ago
  When the Administrator tries to include it in the report
  Then the system warns that the 7-day deadline has passed

AC-27 (FR-17) — Inventory outflow
  Given an ACCEPTED Document with 10 units of Product P-001
  When the acceptance is recorded
  Then there is an outflow Inventory movement of 10 units linked to the Document

AC-61 (FR-45) — Exception code keeps the number
  Given F001-50 is SIGNED and the SUNAT validator answers code 0109 (service not available)
  When the dispatcher records the answer
  Then the status becomes PENDING_RESEND, F001-50 is resent later with the same signed file and no new number is assigned

AC-62 (FR-45) — Rejection code consumes the number
  Given F001-51 is SENT and the CDR carries code 2800
  When the CDR is recorded
  Then the status becomes REJECTED, the code and description are shown and the next Invoice in F001 receives F001-52

AC-63 (FR-45) — Observation code means accepted
  Given F001-53 is SENT and the CDR carries code 0 with observation 4287
  When the CDR is recorded
  Then the status becomes OBSERVED, the Document counts as issued and observation 4287 is listed

AC-64 (FR-46) — Dollar invoice without exchange rate
  Given an Invoice in USD without exchange rate
  When the Salesperson tries to issue it
  Then the request is rejected with 400 and no number is assigned

AC-65 (FR-46) — Dollar invoice with exchange rate
  Given an Invoice in USD with exchange rate 3.745 for its issue date
  When the UBL XML is generated
  Then cac:PaymentExchangeRate has SourceCurrencyCode USD, TargetCurrencyCode PEN and CalculationRate 3.745

AC-66 (FR-47) — Free-of-charge line with a price
  Given an Item with IGV treatment 15 (bonus) and Unit value 2.50
  When the Salesperson tries to issue it
  Then the request is rejected with 400 "A free-of-charge line has unit value 0.00"

AC-67 (FR-47) — Free-of-charge line amounts
  Given an Item with IGV treatment 15, 2 units and Reference unit value 3.00
  When the system calculates
  Then the line reports TaxScheme 9996 with taxable amount 6.00 and IGV 1.08, the total amount does not change and legend 1002 is present

AC-68 (FR-48) — Fixed-amount ISC per litre
  Given 12 bottles of 620 ml (iscUnitsPerItem 0.62) of a Product with ISC system 02 and S/ 1.50 per litre
  When the system calculates
  Then ISC = 11.16 (12 × 0.62 × 1.50)

AC-69 (FR-49) — Two tills never collide
  Given till 1 issues in Series F001 and till 2 in Series F002 of the same establishment
  When both issue an Invoice at the same moment
  Then each Invoice receives the next number of its own Series and no number is repeated

AC-70 (FR-49) — Offline queue does not number
  Given the Android app has no connection and the Salesperson confirms a sale
  When the request is queued locally with its Idempotency-Key
  Then no Series or Sequence number is shown until the server answers, and sending the same request twice returns the same Document

AC-71 (FR-50) — Slow validator does not block the Series
  Given the SUNAT validator takes 25 s to answer F001-60
  When another Salesperson issues an Invoice in F001 during those 25 s
  Then it receives F001-61 without waiting for the first answer

AC-72 (FR-50) — Crash after signing
  Given F001-62 was committed as SIGNED with its outbox entry and the server stops before sending
  When the server restarts
  Then the dispatcher sends F001-62 with the stored signed file and the number is not lost

AC-73 (FR-51) — Series of the wrong letter
  Given the Administrator creates Series X001 for document type 01
  When the Series is saved
  Then the system rejects it with 400

AC-74 (FR-51) — Sequence cannot go back
  Given Series F001 has last Sequence number 62
  When the Administrator tries to set it to 40
  Then the system rejects it with 409 and the Series keeps 62

AC-77 (FR-52) — A SUNAT rejection rule stops issuance
  Given a confirmed Invoice whose XML validates against the XSD but whose Buyer has Identity document type 1
  When the SUNAT rules are checked
  Then the process stops with SUNAT_RULES and code 2800, and nothing is signed or sent

AC-78 (FR-52) — A SUNAT observation rule does not stop issuance
  Given a confirmed Invoice whose item IGV differs by more than 1.00 from 18 % of its base
  When the SUNAT rules are checked
  Then the Document is signed and sent, and observation 4290 is recorded with it

## Out of scope
- Dispatch guides (*guía de remisión*), withholding and perception documents, purchase settlements.
- Detractions and export operations (catalog 51 other than 0101).
- Factoring and Buyer conformity of invoices.
- Online payments or payment gateway.

## Clarifications
- Resolved ([D-13](../../clarifications.md)): SEE-Del contribuyente, directly to SUNAT; an OSE is another adapter chosen by configuration.
- Resolved ([D-14](../../clarifications.md)): own Digital certificate, kept only on the server (ADR 0006).
- Resolved ([D-15](../../clarifications.md)): ISC is Product configuration set by the Administrator; initial catalog beer, spirits and sugary drinks as in `products.json`.
- Resolved ([D-16](../../clarifications.md)): yes, plastic bags are handed out at every till (ICBPER S/ 0.50 per bag).
- Resolved (FR-46): USD sales use SUNAT's published *tipo de cambio venta* of the issue date. Resolved ([D-17](../../clarifications.md)): USD is allowed but optional, PEN by default.
- Resolved (FR-49, [ADR 0008](../../adr/0008-server-numbering-series-per-point-of-issue.md)): one Series per document type and point of issue. Resolved ([D-18](../../clarifications.md)): configurable; initially 1 establishment with 2 tills (F001/B001, F002/B002).
- Resolved ([D-19](../../clarifications.md)): discounts per Item only (catalog 53 code 00); global discounts are out of scope of this version.
- Resolved (FR-49, ADR 0008): the app queues requests offline but never issues offline; when the server itself cannot reach SUNAT the Document is issued and stays PENDING_RESEND; paper Contingency (FR-16) is only for a failure of the whole system.
