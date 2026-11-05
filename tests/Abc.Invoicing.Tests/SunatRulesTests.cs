using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;
using Abc.Invoicing.Infrastructure.Ubl;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>
/// FR-52: SUNAT's validation rules that the XSD cannot express, checked on the generated XML before signing.
/// Each case breaks one rule of the regulatory matrix in an XML that is otherwise valid and expects SUNAT's code.
/// </summary>
public class SunatRulesTests
{
    private static readonly Ubl21InvoiceGenerator Generator = new();
    private static readonly SunatRulesValidator Rules = new();

    private static ElectronicDocument Credit43() => Examples.Document("issue-invoice-credit.json", 43, "11:02:10");
    private static ElectronicDocument Usd45() => Examples.Document("issue-invoice-usd.json", 45, "16:45:00");
    private static ElectronicDocument Free10() => Examples.Document("issue-receipt-free-line.json", 10, "18:20:00");

    private const string Buyer = "/inv:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID";
    private const string Cuotas = "/inv:Invoice/cac:PaymentTerms[starts-with(cbc:PaymentMeansID,'Cuota')]";

    /// <summary>name → (document, how the XML is broken, SUNAT code expected).</summary>
    private static readonly Dictionary<string, (Func<ElectronicDocument> Doc, Action<Xml> Break, string Code)> Cases = new()
    {
        ["UBLVersionID 2.0"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cbc:UBLVersionID", "2.0"), "2074"),
        ["CustomizationID 1.0"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cbc:CustomizationID", "1.0"), "2072"),
        ["Invoice with a B series"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cbc:ID", "B001-42"), "1001"),
        ["Operation type missing"] = (Examples.Cash42, x => x.Node("/inv:Invoice/cbc:InvoiceTypeCode")!.RemoveAttribute("listID"), "3205"),
        ["Operation type outside catalog 51"] = (Examples.Cash42, x => x.SetAttr("/inv:Invoice/cbc:InvoiceTypeCode", "listID", "9999"), "3206"),
        ["An amount in another currency"] = (Examples.Cash42, x => x.SetAttr("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", "currencyID", "USD"), "2071"),
        ["USD without exchange rate"] = (Usd45, x => x.Remove("/inv:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate"), "2721"),
        ["USD with exchange rate 0"] = (Usd45, x => x.Set("/inv:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate", "0"), "2716"),
        ["Invoice to a DNI"] = (Examples.Cash42, x => x.SetAttr(Buyer, "schemeID", "1"), "2800"),
        ["Buyer RUC with a wrong check digit"] = (Examples.Cash42, x => x.Set(Buyer, "20600123451"), "2802"),
        ["Repeated item number"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:InvoiceLine[2]/cbc:ID", "1"), "2752"),
        ["Quantity 0"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:InvoiceLine[1]/cbc:InvoicedQuantity", "0"), "2024"),
        ["Legend 1000 twice"] = (Examples.Cash42, x =>
        {
            var note = x.Node("/inv:Invoice/cbc:Note[@languageLocaleID='1000']")!;
            note.ParentNode!.InsertAfter(note.CloneNode(true), note);
        }, "3014"),
        ["Free line without the 9996 total"] = (Free10, x => x.Remove("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']"), "2641"),
        ["Legend 1002 without the 9996 total"] = (Free10, x => x.Remove("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']"), "2416"),
        ["Invoice without FormaPago"] = (Examples.Cash42, x => x.Remove("/inv:Invoice/cac:PaymentTerms"), "3244"),
        ["Credit without installments"] = (Credit43, x => { foreach (var q in x.Nodes(Cuotas)) q.ParentNode!.RemoveChild(q); }, "3249"),
        ["Credit without outstanding amount"] = (Credit43, x => x.Remove("/inv:Invoice/cac:PaymentTerms[cbc:PaymentMeansID='Credito']/cbc:Amount"), "3251"),
        ["Outstanding amount above the total"] = (Credit43, x => x.Set("/inv:Invoice/cac:PaymentTerms[cbc:PaymentMeansID='Credito']/cbc:Amount", "9999.00"), "3265"),
        ["Installment above the total"] = (Credit43, x => x.Set(Cuotas + "[1]/cbc:Amount", "9999.00"), "3266"),
        ["Installment due before the issue date"] = (Credit43, x => x.Set(Cuotas + "[1]/cbc:PaymentDueDate", "2020-01-01"), "3267"),
        ["Signature reference without ID"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:Signature/cbc:ID", ""), "2076"),
        ["Item sale value ≠ quantity × unit value"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:InvoiceLine[1]/cbc:LineExtensionAmount", "55.00"), "4288"),
        ["Item IGV ≠ 18 % of its base"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:InvoiceLine[1]/cac:TaxTotal/cac:TaxSubtotal/cbc:TaxAmount", "12.00"), "4290"),
        ["Total taxes ≠ Σ tax subtotals"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:TaxTotal/cbc:TaxAmount", "15.00"), "4301"),
        ["Total sale value ≠ Σ items"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount", "80.00"), "4309"),
        ["TaxInclusiveAmount ≠ sale value + taxes"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount", "90.00"), "4211"),
        ["PayableAmount ≠ calculated total"] = (Examples.Cash42, x => x.Set("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", "90.00"), "4312"),
    };

    public static IEnumerable<object[]> CaseNames() => Cases.Keys.Select(k => new object[] { k });

    [Theory(DisplayName = "AC-77 The five reference invoices break no SUNAT rule")]
    [MemberData(nameof(Examples.InvoiceCases), MemberType = typeof(Examples))]
    public void Ac77_reference_invoices_break_no_rule(string reference, string request, int number, string time)
    {
        var violations = Rules.Validate(Generator.Generate(Examples.Document(request, number, time)));
        Assert.True(violations.Count == 0, reference + ": " + string.Join(" | ", violations));
    }

    [Theory(DisplayName = "AC-77 / AC-78 Each broken SUNAT rule returns SUNAT's code")]
    [MemberData(nameof(CaseNames))]
    public void Ac77_each_rule_returns_its_sunat_code(string name)
    {
        var (doc, breakIt, code) = Cases[name];
        var x = new Xml(Generator.Generate(doc()));
        breakIt(x);
        var violations = Rules.Validate(x.Bytes());
        Assert.Contains(violations, v => v.Code == code);
        Assert.Equal(int.Parse(code) >= 4000, violations.First(v => v.Code == code).IsObservation);
    }

    [Fact(DisplayName = "AC-77 A rule with a 2000–3999 code stops issuance: nothing is signed or sent")]
    public async Task Ac77_rejection_rule_stops_before_signing()
    {
        var sent = false;
        var useCase = new IssueDocument(new Breaking(x => x.SetAttr(Buyer, "schemeID", "1")), new OasisXsdValidator(), Rules,
            new Infrastructure.Signing.XmlDsigSigner(ValidationAndSigningTests.TestCertificate()), new Sunat(() => sent = true));
        var ex = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Examples.Cash42()));
        Assert.Equal("SUNAT_RULES", ex.Code);
        Assert.Contains("2800", ex.Message);
        Assert.False(sent);
    }

    [Fact(DisplayName = "AC-78 A rule with a 4000+ code is an observation: the document is signed, sent and the observation kept")]
    public async Task Ac78_observation_rule_still_sends()
    {
        var sent = false;
        var useCase = new IssueDocument(
            new Breaking(x => x.Set("/inv:Invoice/cac:InvoiceLine[1]/cac:TaxTotal/cac:TaxSubtotal/cbc:TaxAmount", "12.00")),
            new OasisXsdValidator(), Rules, new Infrastructure.Signing.XmlDsigSigner(ValidationAndSigningTests.TestCertificate()),
            new Sunat(() => sent = true));
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.True(sent);
        Assert.Contains(result.Observations, v => v.Code == "4290" && v.IsObservation);
    }

    private sealed class Breaking(Action<Xml> breakIt) : IUblGenerator
    {
        public byte[] Generate(ElectronicDocument document)
        {
            var x = new Xml(Generator.Generate(document));
            breakIt(x);
            return x.Bytes();
        }
    }

    private sealed class Sunat(Action onSend) : ISunatValidator
    {
        public Task<CdrResult> SendBillAsync(string fileName, byte[] signedXml, CancellationToken ct = default)
        {
            onSend();
            return Task.FromResult(new CdrResult(0, "aceptada", [], null));
        }
    }
}
