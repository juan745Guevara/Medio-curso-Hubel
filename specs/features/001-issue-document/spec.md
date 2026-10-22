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
- **FR-01** (Ubiquitous): The system shall assign each Document a unique Series and Sequence number, consecutive per Series, formatted `SSSS-NNNNNNNN`, where an Invoice Series starts with F and a Sales receipt Series with B.
- **FR-02** (Event-driven): When the Salesperson enters an Identity document of type 6, the system shall validate that the RUC has 11 digits, prefix 10, 15, 16, 17 or 20 and a correct modulo-11 check digit before allowing issuance.
- **FR-03** (Unwanted behaviour): If the Salesperson tries to issue an Invoice to a Buyer without a valid RUC, then the system shall block issuance and state that an Invoice requires a RUC.
- **FR-04** (State-driven): While the total amount of a Sales receipt exceeds S/ 700.00, the system shall require the Buyer's Identity document type, number and name.
- **FR-05** (Ubiquitous): The system shall calculate per Item the sale value, the ISC according to the Product's ISC system (catalog 08), the IGV on sale value plus ISC for taxed Items (catalog 07 code 10), and the ICBPER per bag outside the IGV base.
- **FR-06** (Ubiquitous): The system shall calculate the Document totals: total per IGV treatment (taxed, exempt, non-taxable), total IGV, total ISC, total ICBPER, total discounts and total amount, rounded to 2 decimals.
- **FR-07** (Event-driven): When the Salesperson confirms issuance, the system shall generate the UBL 2.1 XML with `UBLVersionID` 2.1 and `CustomizationID` 2.0, the catalog 51 Operation type and the mandatory data of Annex 9-A.
- **FR-08** (Unwanted behaviour): If the UBL XML does not validate against the OASIS XSD, then the system shall stop the process, neither sign nor send, and log the structure error.
- **FR-09** (Ubiquitous): The system shall sign the UBL XML with the Issuer's Digital certificate using XMLDSig enveloped inside `ext:UBLExtensions` before sending it.
- **FR-10** (Event-driven): When a Document becomes SIGNED, the system shall send it to the configured SUNAT validator (SUNAT or OSE) in a ZIP named `RUC-TT-SERIES-NUMBER`, and record the CDR with its code and description.
- **FR-11** (Unwanted behaviour): If the SUNAT validator does not respond or there is a network failure, then the system shall leave the Document in PENDING_RESEND, retry automatically and alert when 1 day remains before the submission deadline.
- **FR-12** (Ubiquitous): The system shall record the Invoice Payment terms: Cash, or Credit with Outstanding amount and each Installment with amount and due date.
- **FR-13** (Event-driven): When the Document is ACCEPTED or OBSERVED, the system shall generate the Printed representation with a QR code and allow downloading it together with the XML and the CDR.
- **FR-14** (Optional feature): Where the Buyer has a registered e-mail address, the system shall send them the Printed representation and the XML once ACCEPTED.
- **FR-15** (Ubiquitous): The system shall include in the UBL XML the legend with the total amount in words (catalog 52, code 1000), written in Spanish as SUNAT requires.
- **FR-16** (Optional feature): Where the Administrator has activated Contingency due to a documented failure, the system shall allow recording paper contingency Documents and generate their report for SUNAT within 7 calendar days.
- **FR-17** (Ubiquitous): The system shall deduct from inventory the quantity of each Item of an ACCEPTED or OBSERVED Document as an outflow Inventory movement linked to the Document.

## Non-functional requirements
- **NFR-01**: Issuing an Invoice of up to 50 Items, from confirmation to signed XML, shall take ≤ 2 s at the 95th percentile.
- **NFR-02**: 100 % of the XML sent shall have passed XSD validation (verifiable in the audit log).
- **NFR-03**: Invoices shall be sent to the SUNAT validator within the current deadline (up to 3 calendar days counted from the day after issuance); the system shall report 0 overdue Invoices without a prior alert.
- **NFR-04**: The application shall run on Windows 10 or later installed via MSI and on Android 10 or later installed via APK.
- **NFR-05**: The Digital certificate and its password shall be stored encrypted and never appear in logs or responses.
- **NFR-06**: XML, CDR and Printed representation shall be kept for at least 5 years without modification (verifiable hash).
- **NFR-07**: Switching between SUNAT and OSE, or between test (beta) and production, shall be a configuration change without recompiling.

## Acceptance criteria
AC-01 (FR-01) — Consecutive sequence per Series
  Given the last Invoice in Series F001 is F001-00000041
  When the Salesperson issues a new Invoice in F001
  Then the Document receives F001-00000042

AC-02 (FR-01) — Sequence number is never reused
  Given F001-00000042 was REJECTED by the SUNAT validator
  When the Salesperson issues another Invoice in F001
  Then it receives F001-00000043 and F001-00000042 stays recorded as REJECTED

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

## Out of scope
- Dispatch guides (*guía de remisión*), withholding and perception documents, purchase settlements.
- Detractions and export operations (catalog 51 other than 0101).
- Factoring and Buyer conformity of invoices.
- Online payments or payment gateway.

## Clarifications
- [NEEDS CLARIFICATION] Will the Issuer send directly to SUNAT (SEE-Del contribuyente) or through an OSE? Which OSE?
- [NEEDS CLARIFICATION] Does ABC have its own Digital certificate or will it use a PSE's?
- [NEEDS CLARIFICATION] Which Products are subject to ISC (spirits, beer, cigarettes, sugary drinks) and with which ISC system and rate or amount?
- [NEEDS CLARIFICATION] Are plastic bags handed out at the point of sale (ICBPER)?
- [NEEDS CLARIFICATION] Are sales made in US dollars? If so, which exchange rate is used?
- [NEEDS CLARIFICATION] How many points of issue are there (one Series per till or per store)?
- [NEEDS CLARIFICATION] Do wholesale sales apply volume discounts? Per Item or global?
- [NEEDS CLARIFICATION] Must the app issue offline and send later, or is paper Contingency enough?
