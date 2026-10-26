# Ports (internal and external service contracts)

Deliverable 3 of the exam ("Services" part). These are the interfaces that make it possible to move from MVC to hexagonal without rewriting the domain: today the MVC controllers call the **input ports**; the **output ports** are implemented by adapters. Signatures in logical, language-independent notation.

## Input ports (use cases)

| Port | Operation | Pre | Post | Errors | FR |
|---|---|---|---|---|---|
| `IssueDocument` | `execute(IssueDocumentRequest) → ElectronicDocumentResponse` | Series exists and is active; Products classified; Certificate valid | Document with number assigned, signed XML stored, submission queued or CDR result | INVALID_RUC, INVOICE_REQUIRES_RUC, RECEIPT_REQUIRES_DOCUMENT, CREDIT_WITHOUT_INSTALLMENTS, INSTALLMENTS_MISMATCH, INVALID_XSD, UNCLASSIFIED_PRODUCT | FR-01..FR-15 |
| `IssueNote` | `execute(IssueNoteRequest) → ElectronicDocumentResponse` | Modified document ACCEPTED/OBSERVED | Note issued | MULTIPLE_DOCUMENT_NOTE, DOCUMENT_NOT_ACCEPTED, CREDIT_EXCEEDED | FR-18..FR-22 |
| `SubmitVoiding` | `execute(SubmitVoidingRequest) → BatchSubmissionResponse` | Within the deadline | Ticket recorded | VOIDING_PAST_DEADLINE | FR-23, FR-24 |
| `GenerateDailySummary` | `execute(date) → BatchSubmissionResponse[]` | There are unreported sales receipts/notes | Submissions of ≤ 500 lines with ticket | — | FR-25 |
| `CheckTicket` | `execute(submissionId) → BatchSubmissionResponse` | Submission with ticket | Status updated | — | FR-26 |
| `ResendPending` | `execute() → summary` | — | Retries done; deadline alerts | — | FR-11 |
| `GetDocument` | `execute(id) → ElectronicDocumentResponse` | — | — | NOT_FOUND | FR-27 |
| `MigrateBatch` | `execute(file) → MigrationBatchResponse` | — | Movements created, reconciliation | — | FR-28..FR-35 |
| `GenerateSalesRegister` | `execute(period, format) → SalesRegisterRow[] \| PLE TXT 140100` | — | 35 fields of format 14.1 per document; TXT `LE…140100…` | — | FR-37..FR-39, FR-41, FR-44 |
| `GenerateInventoryRegister` | `execute(period, establishment, format) → StockCard[] \| PLE TXT 120100` | Products classified (Table 5, Table 6) | 19 fields of format 12.1 per movement | PLE_INCOMPLETE_PRODUCT | FR-42, FR-43 |
| `GetStockCard` | `execute(product, period, establishment) → StockCard` | — | Rows with running balance | NOT_FOUND | FR-40 |

## Output ports

| Port | Operation | Initial adapter | Future adapters | Notes |
|---|---|---|---|---|
| `DocumentRepository` | `save`, `byId`, `byNumber`, `byStatus`, `ofPeriod` | Relational database | Own invoicing service | Transaction with `SeriesRepository` |
| `SeriesRepository` | `nextSequence(series) → DocumentNumber` (atomic) | Row lock | — | Constitution 6 |
| `ProductRepository`, `BuyerRepository`, `MovementRepository`, `BatchRepository` | CRUD; `MovementRepository.ofPeriod(product, period, establishment)` ordered by date | Relational database | — | |
| `SunatCatalogPort` | `exists(catalog, code, date)`, `description(...)`, `pleTable(table, code)` | Tables loaded from Annex 8 and from the PLE tables (2, 4, 5, 6, 10, 12, 13) | Catalog API | Effective dates (constitution 9) |
| `UblGeneratorPort` | `generate(ElectronicDocument) → XML` | `UblMapper` + classes generated from the official OASIS UBL 2.1 XSDs (XmlSchemaClassGenerator), serialized with `XmlSerializer`; SUNAT customization applied by the mapper | — | [ADR 0005](../adr/0005-ubl-from-xsd-generated-classes.md); mapping from the [matrix](../traceability/regulatory-matrix.md) |
| `XsdValidatorPort` | `validate(XML, type) → errors[]` | Local OASIS 2.1 XSDs (the same ones the classes are generated from) | — | FR-08, ADR 0003 |
| `SignerPort` | `sign(XML) → SignedXML, hash` | XMLDSig with the certificate loaded from the secret store | HSM / cloud signing | FR-09, [ADR 0006](../adr/0006-certificate-custody.md) |
| `SunatValidatorPort` | `sendDocument(zip, name) → Cdr`; `sendBatch(zip, name) → ticket`; `checkTicket(ticket) → Cdr?` | SUNAT billService SOAP | One adapter per OSE (provider REST/SOAP) | Swappable via config (NFR-07) |
| `PrintedRepresentationPort` | `generatePdf(doc, format) → PDF` | PDF + QR generator | — | FR-13 |
| `NotifierPort` | `sendEmail(to, attachments)` | SMTP | Messaging service | FR-14 |
| `DocumentStoragePort` | `store(name, bytes) → path`, `read(path)` | Server disk | Object storage | NFR-06, 5 years |
| `CertificateStorePort` | `store(pfx, password) → thumbprint`, `current() → certificate`, `retire(thumbprint)` | Encrypted with ASP.NET Core Data Protection, key ring outside the database | HSM / cloud key vault | NFR-05, [ADR 0006](../adr/0006-certificate-custody.md) |
| `ClockPort` | `today()`, `now()` | America/Lima time zone | — | Testable deadline calculation |
| `EventsPort` | `publish(event)` | In memory | Message broker | Cut line for microservices |

## External contract: SUNAT validator (SEE-Del contribuyente)

| Method | Use | Input | Output | Source |
|---|---|---|---|---|
| `sendBill` | Invoice, individual sales receipt, notes | ZIP `RUC-TT-SERIES-NUM.zip` in base64 | ZIP with CDR (synchronous) | [Annex 244-2019](https://ww3.sunat.gob.pe/legislacion/superin/2019/anexo-244-2019.pdf) |
| `sendSummary` | Daily summary (RC) and Voiding notice (RA) | ZIP in base64 | Ticket | same |
| `getStatus` | Check ticket | Ticket | Status + CDR | same |
| Authentication | WS-Security UsernameToken | RUC+SOL user, SOL key | — | same |
| Endpoints | Beta: `e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService`; Production: `e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService` | — | — | [greenter/ws](https://packagist.org/packages/greenterfin/ws) (to be verified with SUNAT) |

With an OSE, each operator defines the contract; the adapter translates it to the same `SunatValidatorPort`.
