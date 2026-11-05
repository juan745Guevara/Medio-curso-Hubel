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
| 001 | US-04 | FR-45 | AC-61 | UC-08 | `Ac61_ExceptionCodeKeepsTheNumber()` |
| 001 | US-04 | FR-45 | AC-62 | UC-08 | `Ac62_RejectionCodeConsumesTheNumber()` |
| 001 | US-04 | FR-45 | AC-63 | UC-08 | `Ac63_ObservationCodeMeansAccepted()` |
| 001 | US-01 | FR-46 | AC-64 | UC-01 | `Ac64_DollarInvoiceWithoutExchangeRate()` + example `invalid/issue-usd-without-exchange-rate.json` |
| 001 | US-01 | FR-46 | AC-65 | UC-01 | `Ac65_DollarInvoiceWithExchangeRate()` + XML `20131312955-01-F001-45.xml` |
| 001 | US-03 | FR-47 | AC-66 | UC-02 | `Ac66_FreeOfChargeLineWithAPrice()` + example `invalid/issue-free-line-with-value.json` |
| 001 | US-03 | FR-47 | AC-67 | UC-02 | `Ac67_FreeOfChargeLineAmounts()` + XML `20131312955-03-B001-10.xml` |
| 001 | US-03 | FR-48 | AC-68 | UC-01 | `Ac68_FixedAmountIscPerLitre()` + XML `20131312955-01-F001-44.xml` |
| 001 | US-06 | FR-49 | AC-69 | UC-01 | `Ac69_TwoTillsNeverCollide()` |
| 001 | US-06 | FR-49 | AC-70 | UC-01 | `Ac70_OfflineQueueDoesNotNumber()` |
| 001 | US-04 | FR-50 | AC-71 | UC-01 | `Ac71_SlowValidatorDoesNotBlockTheSeries()` |
| 001 | US-04 | FR-50 | AC-72 | UC-08 | `Ac72_CrashAfterSigning()` |
| 001 | US-06 | FR-51 | AC-73 | UC-11 | `Ac73_SeriesOfTheWrongLetter()` + example `invalid/series-wrong-letter.json` |
| 001 | US-06 | FR-51 | AC-74 | UC-11 | `Ac74_SequenceCannotGoBack()` |
| 001 | US-04 | FR-11 | AC-79 | UC-08 | `Ac79_CdrFoundIsNotResent()` |
| 001 | US-04 | FR-11 | AC-80 | UC-08 | `Ac80_NoCdrResendsTheSameSignedFile()` |
| 001 | US-01 | FR-52 | AC-77 | UC-01 | `Ac77_SunatRejectionRuleStopsIssuance()` |
| 001 | US-01 | FR-52 | AC-78 | UC-01 | `Ac78_SunatObservationRuleDoesNotStop()` |
| 002 | US-08 | FR-18 | AC-75 | UC-05 | `Ac75_DebitNoteIsItsOwnDocument()` + XML `20131312955-08-FD01-1.xml` |
| 002 | US-09 | FR-23 | AC-76 | UC-06 | `Ac76_VoidingNoticeXml()` + XML `20131312955-RA-20261021-1.xml` |

Total: 76 AC. The JSON examples and reference XML named in the Test column live in [`specs/examples/`](../examples/) and are checked by `python tools/validate_specs.py` on every change.

## Implemented tests (second exam, `tests/Abc.Invoicing.Tests`, xUnit)

The C# names above are the target names of the full product; the second exam implements the XML generation, validation, signature and sending, and these are the tests that already run with `dotnet test`:

| AC | Implemented test (class → method) |
|---|---|
| AC-01 | `GenerationTests.Ac01_id_and_file_name` |
| AC-03, AC-04 | `DomainTests.Ac03_ruc` |
| AC-05 | `DomainTests.Ac05_invoice_requires_ruc` |
| AC-06 | `DomainTests.Ac06_receipt_over_700` |
| AC-08, AC-12 | `DomainTests.Ac08_taxed_item`, `GenerationTests.Ac08_ac12_lines`, `DomainTests.Unit_outside_catalog_03` |
| AC-09, AC-10, AC-68 | `DomainTests.Ac09_ad_valorem_isc`, `DomainTests.Ac10_fixed_amount_isc`, `DomainTests.Ac68_isc_per_litre`, `GenerationTests.Ac09_isc_systems_in_tier_range` |
| AC-11 | `DomainTests.Ac11_icbper`, `GenerationTests.Ac11_icbper` |
| AC-13 | `DomainTests.Ac13_totals`, `GenerationTests.Ac13_totals` |
| AC-14 | `GenerationTests.Ac14_header` |
| AC-15 | `ValidationAndSigningTests.Ac15_missing_issue_date_is_rejected`, `SendingTests.Nothing_is_sent_when_invalid` |
| AC-16 | `ValidationAndSigningTests.Ac16_signature`, `…Signature_value_matches_in_place_canonicalization`, `…Altered_signed_xml_does_not_verify` |
| AC-17 | `SendingTests.Ac17_accepted`; against the real service: `SunatBetaTests.Ac17_sunat_beta_accepts_the_invoice` and `…Ac17_sunat_beta_accepts_the_sales_receipt` (run with `SUNAT_BETA=1`; accepted with code 0 on 2026-11-04, CDR in [`examples/sunat-beta/`](../examples/sunat-beta/README.md)) |
| AC-19 | `PendingSubmissionTests.Ac19_no_answer_leaves_pending_resend` |
| AC-79, AC-80 | `PendingSubmissionTests.Ac79_cdr_found_is_not_resent`, `…Ac79_rejected_cdr_found`, `…Ac80_no_cdr_resends_the_same_signed_file`, `…GetStatusCdr_envelope`, `…Query_exception_keeps_pending`, `…Query_without_answer_keeps_pending` |
| AC-21, AC-22 | `GenerationTests.Ac21_payment_terms`, `DomainTests.Ac21_ac22_credit_rules` |
| AC-25 | `GenerationTests.Ac25_amount_in_words`, `DomainTests.Ac25_amount_in_words` |
| AC-31 | `DomainTests.Receipt_with_f_series_is_refused` |
| AC-61, AC-62, AC-63 | `SendingTests.Ac61_exception_keeps_pending`, `SendingTests.Ac62_rejected`, `SendingTests.Ac63_observed`, `DomainTests.Fr45_return_codes` |
| AC-64, AC-65 | `DomainTests.Ac64_usd_requires_exchange_rate`, `GenerationTests.Ac65_usd_exchange_rate` |
| AC-66, AC-67 | `DomainTests.Ac66_free_line_with_value`, `GenerationTests.Ac67_free_of_charge_line` |
| AC-77, AC-78 | `SunatRulesTests.Ac77_reference_invoices_break_no_rule` (5), `SunatRulesTests.Ac77_each_rule_returns_its_sunat_code` (28 rules), `SunatRulesTests.Ac77_rejection_rule_stops_before_signing`, `SunatRulesTests.Ac78_observation_rule_still_sends` |
| FR-08 (all cases) | `GenerationTests.Generated_xml_is_valid_against_the_official_xsd`, `ReferenceXmlTests.*` |
| ADR 0001 | `ArchitectureTests.*` |

