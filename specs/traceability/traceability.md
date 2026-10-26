# Traceability US → FR → AC → Use case → Test

One row per acceptance criterion across all features. Test names follow the C# (xUnit) convention from ADR 0002, with the AC visible. The `qa` agent designs each case; no FR is "done" without its ACs green.

| Feature | US | FR | AC | Use case | Test |
|---|---|---|---|---|---|
| 001 | US-01 | FR-01 | AC-01 | UC-01 | `Ac01_ConsecutiveSequencePerSeries()` |
| 001 | US-01 | FR-01 | AC-02 | UC-01 | `Ac02_SequenceNumberIsNeverReused()` |
| 001 | US-01 | FR-02 | AC-03 | UC-03 | `Ac03_ValidRuc()` |
| 001 | US-01 | FR-02 | AC-04 | UC-03 | `Ac04_RucWithWrongCheckDigit()` |
| 001 | US-01 | FR-03 | AC-05 | UC-01 | `Ac05_InvoiceWithoutRuc()` |
| 001 | US-02 | FR-04 | AC-06 | UC-02 | `Ac06_SalesReceiptOverS700WithoutDocument()` |
| 001 | US-02 | FR-04 | AC-07 | UC-02 | `Ac07_SalesReceiptUnderS700WithoutDocument()` |
| 001 | US-03 | FR-05 | AC-08 | UC-01 | `Ac08_IgvOfATaxedItem()` |
| 001 | US-03 | FR-05 | AC-09 | UC-01 | `Ac09_AdValoremIscBeforeIgv()` |
| 001 | US-03 | FR-05 | AC-10 | UC-01 | `Ac10_FixedAmountIsc()` |
| 001 | US-03 | FR-05 | AC-11 | UC-01 | `Ac11_IcbperOutsideTheIgvBase()` |
| 001 | US-03 | FR-05 | AC-12 | UC-01 | `Ac12_ExemptItem()` |
| 001 | US-03 | FR-06 | AC-13 | UC-01 | `Ac13_DocumentTotals()` |
| 001 | US-01 | FR-07 | AC-14 | UC-01 | `Ac14_UblHeader()` |
| 001 | US-01 | FR-08 | AC-15 | UC-01 | `Ac15_InvalidXmlIsNotSent()` |
| 001 | US-01 | FR-09 | AC-16 | UC-01 | `Ac16_SignaturePresent()` |
| 001 | US-04 | FR-10 | AC-17 | UC-01 | `Ac17_CdrAccepted()` |
| 001 | US-04 | FR-10 | AC-18 | UC-01 | `Ac18_CdrRejected()` |
| 001 | US-04 | FR-11 | AC-19 | UC-08 | `Ac19_NoConnection()` |
| 001 | US-04 | FR-11 | AC-20 | UC-08 | `Ac20_DeadlineAlert()` |
| 001 | US-01 | FR-12 | AC-21 | UC-01 | `Ac21_CreditWithoutInstallments()` |
| 001 | US-01 | FR-12 | AC-22 | UC-01 | `Ac22_InstallmentsDoNotAddUp()` |
| 001 | US-05 | FR-13 | AC-23 | UC-09 | `Ac23_PrintedRepresentation()` |
| 001 | US-05 | FR-14 | AC-24 | UC-09 | `Ac24_EMailDelivery()` |
| 001 | US-01 | FR-15 | AC-25 | UC-01 | `Ac25_AmountInWordsLegend()` |
| 001 | US-06 | FR-16 | AC-26 | UC-10 | `Ac26_ContingencyPastTheDeadline()` |
| 001 | US-01 | FR-17 | AC-27 | UC-01 | `Ac27_InventoryOutflow()` |
| 002 | US-07 | FR-18 | AC-28 | UC-04 | `Ac28_NoteWithoutReason()` |
| 002 | US-07 | FR-18 | AC-29 | UC-04 | `Ac29_ReturnPerItem()` |
| 002 | US-07 | FR-19 | AC-30 | UC-04 | `Ac30_NoteOnARejectedDocument()` |
| 002 | US-07 | FR-20 | AC-31 | UC-04 | `Ac31_SeriesOfTheNote()` |
| 002 | US-07 | FR-21 | AC-32 | UC-04 | `Ac32_NoteOnTwoDocuments()` |
| 002 | US-07 | FR-22 | AC-33 | UC-04 | `Ac33_ExcessCredit()` |
| 002 | US-09 | FR-23 | AC-34 | UC-06 | `Ac34_VoidingAccepted()` |
| 002 | US-09 | FR-24 | AC-35 | UC-06 | `Ac35_VoidingPastTheDeadline()` |
| 002 | US-10 | FR-25 | AC-36 | UC-07 | `Ac36_SummaryOf620SalesReceipts()` |
| 002 | US-10 | FR-26 | AC-37 | UC-07 | `Ac37_TicketInProgress()` |
| 002 | US-11 | FR-27 | AC-38 | UC-08 | `Ac38_ManualResend()` |
| 003 | US-12 | FR-28 | AC-39 | UC-13 | `Ac39_BatchRecorded()` |
| 003 | US-12 | FR-29 | AC-40 | UC-13 | `Ac40_Inflow()` |
| 003 | US-13 | FR-30 | AC-41 | UC-13 | `Ac41_TraceabilityToTheRow()` |
| 003 | US-12 | FR-31 | AC-42 | UC-13 | `Ac42_NewProduct()` |
| 003 | US-14 | FR-32 | AC-43 | UC-13 | `Ac43_InvalidRecord()` |
| 003 | US-14 | FR-33 | AC-44 | UC-13 | `Ac44_RetryWithoutDuplicates()` |
| 003 | US-13 | FR-34 | AC-45 | UC-13 | `Ac45_Reconciliation()` |
| 003 | US-13 | FR-35 | AC-46 | UC-13 | `Ac46_NroDocumentoIsNotADocument()` |
| 003 | US-12 | FR-36 | AC-47 | UC-12 | `Ac47_UnclassifiedProduct()` |
| 004 | US-15 | FR-37 | AC-48 | UC-14 | `Ac48_PeriodRegister()` |
| 004 | US-15 | FR-38 | AC-49 | UC-14 | `Ac49_CreditNoteSubtracts()` |
| 004 | US-15 | FR-39 | AC-50 | UC-14 | `Ac50_RejectedExcluded()` |
| 004 | US-16 | FR-40 | AC-51 | UC-15 | `Ac51_StockCardWithMigratedData()` |
| 004 | US-15 | FR-41 | AC-52 | UC-14 | `Ac52_Export()` |
| 004 | US-15 | FR-37 | AC-53 | UC-14 | `Ac53_Format141FieldsOfAnInvoice()` |
| 004 | US-15 | FR-38 | AC-54 | UC-14 | `Ac54_ModifiedDocumentOfACreditNote()` |
| 004 | US-15 | FR-37 | AC-55 | UC-14 | `Ac55_DocumentInDollars()` |
| 004 | US-16 | FR-40 | AC-56 | UC-15 | `Ac56_Format121FieldsWithRunningBalance()` |
| 004 | US-15 | FR-41 | AC-57 | UC-14 | `Ac57_PleFileNameOfTheSalesRegister()` |
| 004 | US-16 | FR-42 | AC-58 | UC-14 | `Ac58_InventoryRegisterExport()` |
| 004 | US-16 | FR-43 | AC-59 | UC-14 | `Ac59_ProductWithoutStockType()` |
| 004 | US-15 | FR-44 | AC-60 | UC-14 | `Ac60_VoidedDocumentInTheRegister()` |

Total: 60 AC.
