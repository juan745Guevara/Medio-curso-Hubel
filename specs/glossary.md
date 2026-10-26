# Glossary

Always use these terms, with this meaning, in every file. Peruvian legal terms keep their Spanish name in parentheses because that is how SUNAT, the XML and the professor refer to them.

| Term | Definition |
|---|---|
| **Issuer** (Emisor) | ABC Software Corp as the taxpayer that issues documents; identified by its RUC. |
| **Buyer** (Adquirente) | Customer who receives the document. Identified with an Identity document from catalog 06. |
| **RUC** | Registro Único de Contribuyentes (Peruvian taxpayer ID): 11 digits, prefix 10, 15, 16, 17 or 20, last digit is a modulo-11 check digit. |
| **Identity document** | Pair type (catalog 06) + number of the Buyer: 1 DNI, 4 foreigner ID card, 6 RUC, 7 passport, 0 other. |
| **Document** | Electronic payment document (CPE): Invoice — *Factura* (01) — or Sales receipt — *Boleta de venta* (03). |
| **Note** | Credit note (07) or debit note (08) that modifies a Document. |
| **Electronic document** | A Document or a Note. |
| **Series** (Serie) | 4-character prefix of an Electronic document: F### invoice, B### sales receipt, FC##/BC## notes, etc. |
| **Sequence number** (Correlativo) | Consecutive number of up to 8 digits within a Series. |
| **Item** | Line of an Electronic document: Product, quantity, Unit of measure, unit value and taxes. |
| **Product** | Good sold by the Issuer, with internal code, description, Unit of measure, IGV treatment and, where applicable, ISC system. |
| **Unit of measure** | Code from catalog 03 (UN/ECE Rec. 20), e.g. NIU, KGM, LTR. |
| **Unit value** (Valor unitario) | Price per unit excluding taxes. |
| **Unit price** (Precio unitario) | Price per unit including taxes (catalog 16, code 01). |
| **IGV treatment** (Afectación IGV) | Catalog 07 code stating whether the Item is taxed, exempt, non-taxable or an export. |
| **ISC system** | Catalog 08 code: 01 ad valorem, 02 fixed amount, 03 retail sale price. |
| **IGV** | Impuesto General a las Ventas (Peruvian VAT). The Document shows the total rate of 18 % (IGV + IPM). |
| **ISC** | Impuesto Selectivo al Consumo (excise tax). Part of the IGV base. |
| **ICBPER** | Plastic bag consumption tax, fixed amount per bag, outside the IGV base. |
| **Tax code** (Tributo) | Catalog 05 code (1000 IGV, 2000 ISC, 7152 ICBPER, 9997 exempt, 9998 non-taxable, etc.). |
| **Operation type** | Catalog 51 code (0101 domestic sale). |
| **Payment terms** (Forma de pago) | Cash (*Contado*) or Credit (*Crédito*); if Credit, Outstanding amount and Installments. |
| **Installment** (Cuota) | Amount and due date of a credit payment. |
| **UBL XML** | Electronic document in XML compliant with OASIS UBL 2.1 and SUNAT Annex 9-A. |
| **XSD** | Official OASIS XML schema (`UBL-Invoice-2.1.xsd` and related) used to validate structure. |
| **Digital signature** | XMLDSig enveloped signature of the UBL XML with the Issuer's Digital certificate. |
| **Digital certificate** | Key pair and X.509 certificate of the Issuer used for the Digital signature. |
| **SUNAT validator** | SUNAT or an OSE: receives the UBL XML and returns a CDR. |
| **OSE** | Operador de Servicios Electrónicos: operator authorized by SUNAT to validate Electronic documents. |
| **CDR** | Constancia de Recepción (receipt certificate): Validator response with status accepted, accepted with observations or rejected, plus its code. |
| **SUNAT status** | Life cycle of an Electronic document: DRAFT, VALIDATED, SIGNED, SENT, ACCEPTED, OBSERVED, REJECTED, PENDING_RESEND, VOIDED. |
| **Daily summary** (Resumen diario) | Batch submission reporting Sales receipts and their Notes for one day; returns a Ticket. |
| **Voiding notice** (Comunicación de baja) | Batch submission that voids Electronic documents already reported. |
| **Ticket** | Identifier returned by the SUNAT validator for a batch submission, used to query its CDR. |
| **Contingency** | Exceptional issuance on authorized paper forms when electronic issuance is impossible for reasons not attributable to the Issuer. |
| **Printed representation** | PDF or ticket of the Electronic document for the Buyer, with a QR code. |
| **UUID** | 128-bit technical identifier (e.g. `bc2bf589-f4b1-…`). Identifies operations and records inside the system (ids, `Idempotency-Key`); it is **not** the legal number of a Document, which is Series + Sequence number. |
| **Legacy record** | Row of the previous software: fecha, ingreso, salida, producto, nro_documento (date, inflow, outflow, product, document number). |
| **Migration batch** | Identified migration run, with source, date, counts and hash. |
| **Inventory movement** | Inflow or outflow of a Product with date, quantity and supporting document. |
| **Sales register** | Formal register of the Electronic documents issued in a Period (RS 234-2006), in PLE format 14.1. |
| **Stock card** (kardex) | Movements of one Product in a Period and establishment in PLE format 12.1, with the balance after each movement. |
| **PLE** | Programa de Libros Electrónicos: SUNAT's electronic books (RS 286-2009); each book is a pipe-separated TXT with a fixed name `LE<RUC><AAAAMM00><book>…txt`. |
| **PLE table** | Code tables of the electronic books: 2 identity documents, 4 currencies, 5 stock types, 6 units, 10 document types, 12 operation types, 13 stock catalogs. |
| **CUO** | Código Único de Operación: identifier of the operation in the books (up to 40 characters); in this system, the internal UUID. |
| **Establishment** | Annex establishment registered in the RUC (4-digit code, 0000 = tax address). |
| **Period** | Tax month (YYYY-MM). |
