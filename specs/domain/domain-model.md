# Domain model

Deliverable 1 of the exam ("Domain Model" part). The contracts (DTOs/Schemas) are in [`../contracts/schemas/`](../contracts/schemas/). Logical types, no database-engine types.

## Bounded contexts

Separated from the start so that moving to microservices is just cutting along these lines (see [architecture](../architecture/architecture.md)).

| Context | Responsibility | Aggregates |
|---|---|---|
| **Invoicing** | Issue Electronic documents, calculate taxes, SUNAT status | ElectronicDocument, Series |
| **Catalog** | Products, customers, SUNAT catalogs | Product, Buyer, SunatCatalog |
| **Inventory** | Movements and stock card | InventoryMovement |
| **Migration** | Batches and traceability to legacy data | MigrationBatch |
| **Registers** | Sales register | (read view) |

## Class diagram

```mermaid
classDiagram
  direction LR
  class ElectronicDocument {
    <<aggregate root>>
    +Id id
    +DocumentType type
    +DocumentNumber number
    +Date issueDate
    +Time issueTime
    +Currency currency
    +OperationType operationType
    +SunatStatus status
    +PaymentTerms paymentTerms
    +Totals totals
    +confirm()
    +markSigned(xmlHash)
    +recordCdr(Cdr)
    +void(reason)
  }
  class NoteReference {
    <<value object>>
    +DocumentType modifiedType
    +DocumentNumber modifiedNumber
    +Text reasonCode
    +Text reasonDescription
  }
  class Item {
    <<entity>>
    +Integer order
    +ProductCode product
    +Text description
    +Decimal quantity
    +UnitOfMeasure unit
    +Money unitValue
    +Money unitPrice
    +IgvTreatment igvTreatment
    +IscLine isc
    +Integer icbperBags
    +Taxes taxes
  }
  class Issuer {
    +Ruc ruc
    +Text businessName
    +Address taxAddress
    +Ubigeo ubigeo
    +Text establishmentCode
  }
  class Buyer {
    +IdentityDocument document
    +Text name
    +Address address
    +Email email
  }
  class Series {
    <<aggregate root>>
    +Text code
    +DocumentType type
    +Integer lastSequence
    +next() DocumentNumber
  }
  class Product {
    <<aggregate root>>
    +ProductCode code
    +Text description
    +UnitOfMeasure unit
    +IgvTreatment igvTreatment
    +IscConfig isc
    +Boolean isPlasticBag
    +ProductStatus status
  }
  class PaymentTerms {
    <<value object>>
    +PaymentType type
    +Money outstandingAmount
    +List~Installment~ installments
  }
  class Installment {
    <<value object>>
    +Money amount
    +Date dueDate
  }
  class Totals {
    <<value object>>
    +Money taxed
    +Money exempt
    +Money nonTaxable
    +Money igv
    +Money isc
    +Money icbper
    +Money discounts
    +Money totalAmount
  }
  class Cdr {
    <<value object>>
    +Text code
    +Text description
    +List~Text~ observations
    +DateTime received
  }
  class SignedFile {
    +Text name
    +Text sha256Hash
    +Binary xml
  }
  class InventoryMovement {
    <<aggregate root>>
    +Id id
    +ProductCode product
    +Date date
    +MovementType type
    +Decimal quantity
    +Text supportingDocument
    +MigrationOrigin origin
  }
  class MigrationOrigin {
    <<value object>>
    +Id batchId
    +Integer row
    +Text originalContent
  }
  class MigrationBatch {
    <<aggregate root>>
    +Id id
    +Text file
    +Text fileHash
    +Integer rowsRead
    +Integer migrated
    +Integer rejected
    +Integer duplicates
  }
  ElectronicDocument "1" *-- "1..*" Item
  ElectronicDocument "1" *-- "0..1" NoteReference
  ElectronicDocument "1" *-- "1" PaymentTerms
  ElectronicDocument "1" *-- "1" Totals
  ElectronicDocument "1" *-- "0..1" Cdr
  ElectronicDocument "1" *-- "0..1" SignedFile
  ElectronicDocument --> Issuer
  ElectronicDocument --> Buyer
  ElectronicDocument ..> Series : numbered by
  PaymentTerms *-- Installment
  Item ..> Product
  InventoryMovement *-- "0..1" MigrationOrigin
  MigrationOrigin ..> MigrationBatch
  InventoryMovement ..> ElectronicDocument : supports
```

## Value objects and their rules

| Value object | Representation | Invariant | Source |
|---|---|---|---|
| `Ruc` | 11-digit text | Prefix 10/15/16/17/20 and modulo-11 check digit | FR-02 |
| `IdentityDocument` | type (cat. 06) + number | DNI 8 digits; RUC = `Ruc`; foreigner ID and passport up to 12 alphanumeric | FR-02, FR-04 |
| `DocumentNumber` | Series (4) + Sequence (1..99,999,999) | Series F### / B### by type; notes use the letter of the modified document | FR-01, FR-20 |
| `Money` | Exact decimal + Currency | Never floating point; half-up rounding to 2 decimals in totals | Constitution 7 |
| `Currency` | ISO 4217 code (cat. 02) | PEN or USD | FR-07 |
| `UnitOfMeasure` | UN/ECE Rec 20 code (cat. 03) | Must exist in the catalog | FR-07 |
| `IgvTreatment` | Cat. 07 code | 10–17 taxed, 20–21 exempt, 30–37 non-taxable, 40 export | FR-05 |
| `IscConfig` / `IscLine` | System (cat. 08) + rate or fixed amount | 01 → rate %; 02 → amount per unit; 03 → rate on retail sale price | FR-05 |
| `OperationType` | Cat. 51 code | 0101 in this scope | FR-07 |
| `PaymentTerms` | Cash / Credit | Credit ⇒ ≥ 1 Installment and Σ installments = outstandingAmount | FR-12 |

## Electronic document life cycle

```mermaid
stateDiagram-v2
  [*] --> DRAFT
  DRAFT --> VALIDATED : business rules OK
  VALIDATED --> DRAFT : XSD fails (FR-08)
  VALIDATED --> SIGNED : XMLDSig signature
  SIGNED --> SENT : sendBill / summary
  SENT --> ACCEPTED : CDR code 0
  SENT --> OBSERVED : CDR with observations
  SENT --> REJECTED : rejection CDR
  SENT --> PENDING_RESEND : no response
  PENDING_RESEND --> SENT : retry
  ACCEPTED --> VOIDED : voiding notice accepted
  OBSERVED --> VOIDED : voiding notice accepted
  REJECTED --> [*]
  VOIDED --> [*]
```

## Domain services

| Service | What it does | FR |
|---|---|---|
| `TaxCalculator` | Calculates sale value, ISC (cat. 08), IGV, ICBPER and totals; pure, no dependencies | FR-05, FR-06 |
| `RucValidator` | Modulo 11 | FR-02 |
| `IssuancePolicy` | Pre-issuance rules: invoice needs RUC, sales receipt > S/ 700, credit with installments, classified product | FR-03, FR-04, FR-12, FR-36 |
| `NotePolicy` | One note one document, accepted document, credit cap | FR-19, FR-21, FR-22 |
| `SunatDeadlines` | Deadlines for submission, voiding, summary and contingency | FR-11, FR-16, FR-24, FR-25 |
| `AmountInWordsConverter` | Legend 1000 (in Spanish) | FR-15 |

## Domain events
`DocumentConfirmed`, `DocumentSigned`, `CdrReceived`, `DocumentAccepted`, `DocumentRejected`, `DocumentVoided`, `BatchMigrated`. Published in memory today; they are the cut line for future microservices.
