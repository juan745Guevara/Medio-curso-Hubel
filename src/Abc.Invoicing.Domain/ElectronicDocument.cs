using System.Text.RegularExpressions;

namespace Abc.Invoicing.Domain;

/// <summary>
/// Aggregate root of the Invoicing context: an Invoice (01) or Sales receipt (03) already numbered by the server
/// (FR-49). The constructor enforces the invariants of spec 001; once built it is immutable (constitution 5).
/// </summary>
public sealed partial class ElectronicDocument
{
    public const decimal ReceiptIdentificationThreshold = 700.00m;

    public DocumentType Type { get; }
    public string Series { get; }
    public int Number { get; }
    public DateOnly IssueDate { get; }
    public TimeOnly IssueTime { get; }
    public string Currency { get; }
    public decimal? ExchangeRate { get; }
    public string OperationType { get; }
    public Issuer Issuer { get; }
    public Buyer Buyer { get; }
    public PaymentTerms PaymentTerms { get; }
    public IReadOnlyList<Line> Lines { get; }
    public Totals Totals { get; }

    /// <summary>cbc:ID: Series-Sequence without leading zeros (FR-01), e.g. F001-42.</summary>
    public string Id => $"{Series}-{Number}";

    /// <summary>File name RUC-TT-SERIES-NUMBER (Annex A RS 113-2018, FR-10).</summary>
    public string FileName => $"{Issuer.Ruc}-{Type.Code()}-{Id}";

    public ElectronicDocument(DocumentType type, string series, int number, DateOnly issueDate, TimeOnly issueTime,
        string currency, decimal? exchangeRate, string operationType, Issuer issuer, Buyer buyer,
        PaymentTerms paymentTerms, IReadOnlyList<ItemRequest> items)
    {
        if (type is not (DocumentType.Invoice or DocumentType.SalesReceipt))
            throw new DomainException("UNSUPPORTED_TYPE", "Only invoices (01) and sales receipts (03) in this aggregate");
        var letter = type == DocumentType.Invoice ? 'F' : 'B';
        if (!SeriesPattern().IsMatch(series) || series[0] != letter)
            throw new DomainException("INVALID_SERIES", $"Series {series} must match ^{letter}[A-Z0-9]{{3}}$ for type {type.Code()}");
        if (number is < 1 or > 99_999_999)
            throw new DomainException("INVALID_SEQUENCE", "Sequence number must be 1 to 99,999,999");
        if (type == DocumentType.Invoice && !buyer.HasRuc)
            throw new DomainException("INVOICE_REQUIRES_RUC", "An invoice requires the buyer's RUC");
        if (buyer.HasRuc && !Ruc.IsValid(buyer.DocumentNumber))
            throw new DomainException("INVALID_RUC", "Invalid RUC: check digit");
        if (currency is not ("PEN" or "USD"))
            throw new DomainException("CURRENCY_NOT_ALLOWED", "Only PEN and USD in scope");
        if (currency == "USD" && exchangeRate is not > 0)
            throw new DomainException("EXCHANGE_RATE_REQUIRED", "A dollar document requires the exchange rate (FR-46)");
        if (items.Count == 0)
            throw new DomainException("NO_ITEMS", "A document needs at least one item");

        var lines = new List<Line>();
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (!Catalogs.Units.Contains(it.Unit))
                throw new DomainException("INVALID_UNIT", $"Unit {it.Unit} is not in catalog 03");
            if (it.Quantity <= 0)
                throw new DomainException("INVALID_QUANTITY", "Quantity must be greater than 0");
            var treatment = it.IgvTreatment ?? it.Product.IgvTreatment;
            if (!Catalogs.IgvTreatments.Contains(treatment))
                throw new DomainException("INVALID_IGV_TREATMENT", $"{treatment} is not in catalog 07");
            var free = Catalogs.FreeOfChargeTreatments.Contains(treatment);
            if (free && (it.UnitValue != 0m || it.ReferenceUnitValue is not > 0))
                throw new DomainException("FREE_LINE_WITH_VALUE", "A free-of-charge line has unit value 0.00 and a reference value (FR-47)");
            if (!free && it.ReferenceUnitValue is not null)
                throw new DomainException("REFERENCE_VALUE_ON_ONEROUS_LINE", "Only free-of-charge lines carry a reference value");
            lines.Add(TaxCalculator.CalculateLine(i + 1, it));
        }
        var totals = TaxCalculator.Calculate(lines);

        if (type == DocumentType.SalesReceipt && totals.Payable > ReceiptIdentificationThreshold && buyer.DocumentType == "0")
            throw new DomainException("RECEIPT_REQUIRES_DOCUMENT", "A sales receipt over S/ 700 requires the buyer's document (FR-04)");
        if (paymentTerms.IsCredit)
        {
            if (paymentTerms.Installments.Count == 0)
                throw new DomainException("CREDIT_WITHOUT_INSTALLMENTS", "A credit invoice requires installments");
            if (paymentTerms.Installments.Sum(q => q.Amount) != paymentTerms.OutstandingAmount || paymentTerms.OutstandingAmount > totals.Payable)
                throw new DomainException("INSTALLMENTS_MISMATCH", "Installments differ from the outstanding amount");
            if (paymentTerms.Installments.Any(q => q.DueDate < issueDate))
                throw new DomainException("INSTALLMENTS_MISMATCH", "An installment is due before the issue date");
        }

        Type = type; Series = series; Number = number; IssueDate = issueDate; IssueTime = issueTime; Currency = currency;
        ExchangeRate = exchangeRate; OperationType = operationType; Issuer = issuer; Buyer = buyer; PaymentTerms = paymentTerms;
        Lines = lines; Totals = totals;
    }

    [GeneratedRegex("^[FB][A-Z0-9]{3}$")]
    private static partial Regex SeriesPattern();
}
