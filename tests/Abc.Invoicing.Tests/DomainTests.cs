using Abc.Invoicing.Domain;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>Rules the generator relies on: tax calculation (FR-05, FR-06) and the invariants of spec 001.</summary>
public class DomainTests
{
    private static ItemRequest Item(string code, string qty, string value, string? treatment = null, string? reference = null) =>
        new(Examples.Products[code], decimal.Parse(qty), Examples.Products[code].Unit, decimal.Parse(value), treatment,
            reference is null ? null : decimal.Parse(reference));

    [Fact(DisplayName = "AC-08 Taxed item: sale value 50.00, IGV 9.00, amount 59.00")]
    public void Ac08_taxed_item()
    {
        var l = TaxCalculator.CalculateLine(1, Item("P-001", "10", "5.00"));
        Assert.Equal((50.00m, 9.00m, 59.00m), (l.SaleValue, l.Igv, l.SaleValue + l.Igv));
    }

    [Fact(DisplayName = "AC-09 ISC 01 at 25 %: ISC 25.00, IGV base 125.00, IGV 22.50, amount 147.50")]
    public void Ac09_ad_valorem_isc()
    {
        var l = TaxCalculator.CalculateLine(1, Item("P-021", "4", "25.00"));
        Assert.Equal((25.00m, 125.00m, 22.50m), (l.Isc, l.IgvBase, l.Igv));
        Assert.Equal(147.50m, l.SaleValue + l.Isc + l.Igv);
    }

    [Fact(DisplayName = "AC-10 ISC 02: 12 × 1.25 = 15.00")]
    public void Ac10_fixed_amount_isc()
    {
        var p = new Product("X", "X", "NIU", "10", new IscConfig(IscSystem.FixedAmount, FixedAmount: 1.25m));
        Assert.Equal(15.00m, TaxCalculator.CalculateLine(1, new ItemRequest(p, 12m, "NIU", 1m)).Isc);
    }

    [Fact(DisplayName = "AC-68 ISC 02 per litre: 12 × 0.62 × 1.50 = 11.16")]
    public void Ac68_isc_per_litre()
    {
        Assert.Equal(11.16m, TaxCalculator.CalculateLine(1, Item("P-020", "12", "4.00")).Isc);
    }

    [Fact(DisplayName = "AC-11 ICBPER 1.50 outside the IGV base, IGV 0.05")]
    public void Ac11_icbper()
    {
        var l = TaxCalculator.CalculateLine(1, Item("P-030", "3", "0.10"));
        Assert.Equal((1.50m, 0.05m), (l.Icbper, l.Igv));
    }

    [Fact(DisplayName = "AC-13 Totals: taxed 50.00, exempt 20.00, IGV 9.00, total 79.00")]
    public void Ac13_totals()
    {
        var t = Examples.Cash42().Totals;
        Assert.Equal((50.00m, 20.00m, 9.00m, 79.00m), (t.Taxed, t.Exempt, t.Igv, t.Payable));
    }

    [Theory(DisplayName = "AC-03 / AC-04 RUC modulo 11")]
    [InlineData("20131312955", true)]
    [InlineData("20131312954", false)]
    [InlineData("20600123450", true)]
    [InlineData("30131312955", false)]
    [InlineData("2013131295", false)]
    public void Ac03_ruc(string ruc, bool valid) => Assert.Equal(valid, Ruc.IsValid(ruc));

    [Fact(DisplayName = "AC-25 Amount in words")]
    public void Ac25_amount_in_words()
    {
        Assert.Equal("SETENTA Y NUEVE CON 00/100 SOLES", AmountInWords.Of(79.00m, "PEN"));
        Assert.Equal("TRESCIENTOS DIECISEIS CON 95/100 SOLES", AmountInWords.Of(316.95m, "PEN"));
        Assert.Equal("MIL CIEN CON 50/100 SOLES", AmountInWords.Of(1100.50m, "PEN"));
    }

    private static DomainException Fails(Action a) => Assert.Throws<DomainException>(a);

    [Fact(DisplayName = "AC-05 An invoice to a DNI is refused")]
    public void Ac05_invoice_requires_ruc()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(src.Type, "F001", 42, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, new Buyer("1", "45678912", "ROSA QUISPE"), PaymentTerms.Cash(), [Item("P-001", "1", "5.00")]));
        Assert.Equal("INVOICE_REQUIRES_RUC", ex.Code);
    }

    [Fact(DisplayName = "AC-31 A sales receipt with an F series is refused")]
    public void Receipt_with_f_series_is_refused()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(DocumentType.SalesReceipt, "F001", 1, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, new Buyer("1", "45678912", "ROSA QUISPE"), PaymentTerms.Cash(), [Item("P-001", "1", "5.00")]));
        Assert.Equal("INVALID_SERIES", ex.Code);
    }

    [Fact(DisplayName = "AC-64 A dollar invoice without exchange rate is refused")]
    public void Ac64_usd_requires_exchange_rate()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(src.Type, "F001", 45, src.IssueDate, src.IssueTime, "USD", null, "0101",
            src.Issuer, src.Buyer, PaymentTerms.Cash(), [Item("P-001", "1", "1.50")]));
        Assert.Equal("EXCHANGE_RATE_REQUIRED", ex.Code);
    }

    [Fact(DisplayName = "AC-66 A free-of-charge line with a price is refused")]
    public void Ac66_free_line_with_value()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(src.Type, "F001", 46, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, src.Buyer, PaymentTerms.Cash(), [Item("P-002", "2", "2.50", "15", "3.00")]));
        Assert.Equal("FREE_LINE_WITH_VALUE", ex.Code);
    }

    [Fact(DisplayName = "AC-21 / AC-22 Credit without installments, or installments that do not add up, are refused")]
    public void Ac21_ac22_credit_rules()
    {
        var src = Examples.Cash42();
        ElectronicDocument With(PaymentTerms pt) => new(src.Type, "F001", 43, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, src.Buyer, pt, [Item("P-001", "10", "5.00")]);
        Assert.Equal("CREDIT_WITHOUT_INSTALLMENTS", Fails(() => With(PaymentTerms.Credit(59.00m))).Code);
        Assert.Equal("INSTALLMENTS_MISMATCH",
            Fails(() => With(PaymentTerms.Credit(59.00m, new Installment(50.00m, new DateOnly(2026, 11, 20))))).Code);
    }

    [Fact(DisplayName = "AC-06 A sales receipt over S/ 700 without the buyer's document is refused")]
    public void Ac06_receipt_over_700()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(DocumentType.SalesReceipt, "B001", 11, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, new Buyer("0", "-", "CLIENTES VARIOS"), PaymentTerms.Cash(), [Item("P-021", "30", "25.00")]));
        Assert.Equal("RECEIPT_REQUIRES_DOCUMENT", ex.Code);
    }

    [Fact(DisplayName = "AC-08 A unit outside catalog 03 is refused")]
    public void Unit_outside_catalog_03()
    {
        var src = Examples.Cash42();
        var ex = Fails(() => new ElectronicDocument(src.Type, "F001", 47, src.IssueDate, src.IssueTime, "PEN", null, "0101",
            src.Issuer, src.Buyer, PaymentTerms.Cash(), [new ItemRequest(Examples.Products["P-001"], 1m, "UNID", 5m)]));
        Assert.Equal("INVALID_UNIT", ex.Code);
    }

    [Theory(DisplayName = "FR-45 Return codes by range")]
    [InlineData(0, 0, SunatStatus.Accepted)]
    [InlineData(0, 1, SunatStatus.Observed)]
    [InlineData(109, 0, SunatStatus.PendingResend)]
    [InlineData(1033, 0, SunatStatus.PendingResend)]
    [InlineData(2800, 0, SunatStatus.Rejected)]
    [InlineData(3999, 0, SunatStatus.Rejected)]
    [InlineData(4287, 0, SunatStatus.Observed)]
    public void Fr45_return_codes(int code, int observations, SunatStatus expected) =>
        Assert.Equal(expected, ResponseCodeClassifier.Classify(code, observations));
}
