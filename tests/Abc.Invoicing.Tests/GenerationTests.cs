using Abc.Invoicing.Domain;
using Abc.Invoicing.Infrastructure.Ubl;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>
/// Second exam, step 1: XML generation from the UBL-Invoice-2.1.xsd classes (ADR 0005).
/// Every test generates the XML through the real adapter and checks the SUNAT data of the regulatory matrix.
/// </summary>
public class GenerationTests
{
    private readonly Ubl21InvoiceGenerator _generator = new();
    private readonly OasisXsdValidator _xsd = new();

    private Xml Generate(ElectronicDocument doc) => new(_generator.Generate(doc));

    [Theory(DisplayName = "Generated XML is valid against the official UBL-Invoice-2.1.xsd (FR-08)")]
    [MemberData(nameof(Examples.InvoiceCases), MemberType = typeof(Examples))]
    public void Generated_xml_is_valid_against_the_official_xsd(string referenceFile, string request, int number, string time)
    {
        var xml = _generator.Generate(Examples.Document(request, number, time));
        Assert.Empty(_xsd.Validate(xml));
        Assert.NotEmpty(referenceFile);
    }

    [Fact(DisplayName = "AC-14 Header: UBLVersionID 2.1, CustomizationID 2.0, InvoiceTypeCode 01 with listID 0101")]
    public void Ac14_header()
    {
        var x = Generate(Examples.Cash42());
        Assert.Equal("Invoice", x.Document.DocumentElement!.LocalName);
        Assert.Equal("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", x.Document.DocumentElement.NamespaceURI);
        Assert.Equal("2.1", x.Text("/inv:Invoice/cbc:UBLVersionID"));
        Assert.Equal("2.0", x.Text("/inv:Invoice/cbc:CustomizationID"));
        Assert.Equal("01", x.Text("/inv:Invoice/cbc:InvoiceTypeCode"));
        Assert.Equal("0101", x.Attr("/inv:Invoice/cbc:InvoiceTypeCode", "listID"));
        Assert.Equal("urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01", x.Attr("/inv:Invoice/cbc:InvoiceTypeCode", "listURI"));
        Assert.Equal("PEN", x.Text("/inv:Invoice/cbc:DocumentCurrencyCode"));
    }

    [Fact(DisplayName = "AC-01 cbc:ID is Series-Sequence without leading zeros, file name RUC-TT-SERIES-NUMBER")]
    public void Ac01_id_and_file_name()
    {
        var doc = Examples.Cash42();
        var x = Generate(doc);
        Assert.Equal("F001-42", x.Text("/inv:Invoice/cbc:ID"));
        Assert.Equal("20131312955-01-F001-42", doc.FileName);
    }

    [Fact(DisplayName = "Row 6–7: IssueDate yyyy-mm-dd and IssueTime hh:mm:ss (no fractions, no offset)")]
    public void Issue_date_and_time_format()
    {
        var x = Generate(Examples.Cash42());
        Assert.Equal("2026-10-20", x.Text("/inv:Invoice/cbc:IssueDate"));
        Assert.Equal("10:15:00", x.Text("/inv:Invoice/cbc:IssueTime"));
    }

    [Fact(DisplayName = "Rows 13–19: issuer and buyer with schemeID (catalog 06), RegistrationName and establishment code")]
    public void Issuer_and_buyer()
    {
        var x = Generate(Examples.Cash42());
        const string sup = "/inv:Invoice/cac:AccountingSupplierParty/cac:Party";
        Assert.Equal("20131312955", x.Text($"{sup}/cac:PartyIdentification/cbc:ID"));
        Assert.Equal("6", x.Attr($"{sup}/cac:PartyIdentification/cbc:ID", "schemeID"));
        Assert.Equal("ABC SOFTWARE CORP S.A.C.", x.Text($"{sup}/cac:PartyLegalEntity/cbc:RegistrationName"));
        Assert.Equal("0000", x.Text($"{sup}/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:AddressTypeCode"));
        Assert.Equal("150101", x.Text($"{sup}/cac:PartyLegalEntity/cac:RegistrationAddress/cbc:ID"));
        const string cus = "/inv:Invoice/cac:AccountingCustomerParty/cac:Party";
        Assert.Equal("20600123450", x.Text($"{cus}/cac:PartyIdentification/cbc:ID"));
        Assert.Equal("6", x.Attr($"{cus}/cac:PartyIdentification/cbc:ID", "schemeID"));
        Assert.Equal("BODEGA SAN MARTIN S.A.C.", x.Text($"{cus}/cac:PartyLegalEntity/cbc:RegistrationName"));
    }

    [Fact(DisplayName = "AC-13 Totals: taxed 50.00, exempt 20.00, IGV 9.00, payable 79.00")]
    public void Ac13_totals()
    {
        var x = Generate(Examples.Cash42());
        Assert.Equal(9.00m, x.Dec("/inv:Invoice/cac:TaxTotal/cbc:TaxAmount"));
        Assert.Equal(50.00m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='1000']/cbc:TaxableAmount"));
        Assert.Equal(9.00m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='1000']/cbc:TaxAmount"));
        Assert.Equal(20.00m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9997']/cbc:TaxableAmount"));
        Assert.Equal(70.00m, x.Dec("/inv:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount"));
        Assert.Equal(79.00m, x.Dec("/inv:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount"));
        Assert.Equal(79.00m, x.Dec("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"));
        Assert.Equal("PEN", x.Attr("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", "currencyID"));
    }

    [Fact(DisplayName = "Amounts are written with 2 decimals (79.00, never 79)")]
    public void Amounts_have_two_decimals()
    {
        var x = Generate(Examples.Cash42());
        Assert.Equal("79.00", x.Text("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"));
        Assert.Equal("5.00", x.Text("/inv:Invoice/cac:InvoiceLine[1]/cac:Price/cbc:PriceAmount"));
    }

    [Fact(DisplayName = "AC-25 Legend 1000: SETENTA Y NUEVE CON 00/100 SOLES")]
    public void Ac25_amount_in_words()
    {
        var x = Generate(Examples.Cash42());
        Assert.Equal("SETENTA Y NUEVE CON 00/100 SOLES", x.Text("/inv:Invoice/cbc:Note[@languageLocaleID='1000']"));
    }

    [Fact(DisplayName = "AC-08 / AC-12 Line: catalog 07 in TaxExemptionReasonCode, unit in @unitCode, price type 01")]
    public void Ac08_ac12_lines()
    {
        var x = Generate(Examples.Cash42());
        const string l1 = "/inv:Invoice/cac:InvoiceLine[1]";
        Assert.Equal("1", x.Text($"{l1}/cbc:ID"));
        Assert.Equal("NIU", x.Attr($"{l1}/cbc:InvoicedQuantity", "unitCode"));
        Assert.Equal(10m, x.Dec($"{l1}/cbc:InvoicedQuantity"));
        Assert.Equal(50.00m, x.Dec($"{l1}/cbc:LineExtensionAmount"));
        Assert.Equal(5.90m, x.Dec($"{l1}/cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount"));
        Assert.Equal("01", x.Text($"{l1}/cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceTypeCode"));
        Assert.Equal("10", x.Text($"{l1}/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode"));
        Assert.Equal(9.00m, x.Dec($"{l1}/cac:TaxTotal/cbc:TaxAmount"));
        const string l2 = "/inv:Invoice/cac:InvoiceLine[2]";
        Assert.Equal("KGM", x.Attr($"{l2}/cbc:InvoicedQuantity", "unitCode"));
        Assert.Equal("20", x.Text($"{l2}/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode"));
        Assert.Equal("9997", x.Text($"{l2}/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID"));
        Assert.Equal(0.00m, x.Dec($"{l2}/cac:TaxTotal/cbc:TaxAmount"));
    }

    [Fact(DisplayName = "AC-21 / AC-22 Payment terms: Contado, or Credito with Cuota001… and DueDate")]
    public void Ac21_payment_terms()
    {
        var cash = Generate(Examples.Cash42());
        Assert.Equal("FormaPago", cash.Text("/inv:Invoice/cac:PaymentTerms/cbc:ID"));
        Assert.Equal("Contado", cash.Text("/inv:Invoice/cac:PaymentTerms/cbc:PaymentMeansID"));

        var credit = Generate(Examples.Document("issue-invoice-credit.json", 43, "11:02:10"));
        var terms = credit.Nodes("/inv:Invoice/cac:PaymentTerms");
        Assert.Equal(3, terms.Count);
        Assert.Equal("Credito", credit.Text("/inv:Invoice/cac:PaymentTerms[1]/cbc:PaymentMeansID"));
        Assert.Equal(413.00m, credit.Dec("/inv:Invoice/cac:PaymentTerms[1]/cbc:Amount"));
        Assert.Equal("Cuota001", credit.Text("/inv:Invoice/cac:PaymentTerms[2]/cbc:PaymentMeansID"));
        Assert.Equal("2026-11-19", credit.Text("/inv:Invoice/cac:PaymentTerms[2]/cbc:PaymentDueDate"));
        Assert.Equal("Cuota002", credit.Text("/inv:Invoice/cac:PaymentTerms[3]/cbc:PaymentMeansID"));
        Assert.Equal("2026-12-19", credit.Text("/inv:Invoice/cbc:DueDate"));
    }

    [Fact(DisplayName = "AC-09 / AC-10 / AC-68 ISC: catalog 08 systems 01, 02 (per litre) and 03 in cbc:TierRange, before IGV")]
    public void Ac09_isc_systems_in_tier_range()
    {
        var x = Generate(Examples.Document("issue-invoice-isc.json", 44, "09:30:00"));
        string Isc(int line, string what) => $"/inv:Invoice/cac:InvoiceLine[{line}]/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='2000']/{what}";
        string Igv(int line, string what) => $"/inv:Invoice/cac:InvoiceLine[{line}]/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='1000']/{what}";
        // line 1: pisco, system 01, 25 % of 100.00
        Assert.Equal("01", x.Text(Isc(1, "cac:TaxCategory/cbc:TierRange")));
        Assert.Equal(25.00m, x.Dec(Isc(1, "cbc:TaxAmount")));
        Assert.Equal(125.00m, x.Dec(Igv(1, "cbc:TaxableAmount")));
        Assert.Equal(22.50m, x.Dec(Igv(1, "cbc:TaxAmount")));
        // line 2: beer 620 ml, system 02, 12 × 0.62 L × 1.50
        Assert.Equal("02", x.Text(Isc(2, "cac:TaxCategory/cbc:TierRange")));
        Assert.Equal(11.16m, x.Dec(Isc(2, "cbc:TaxAmount")));
        Assert.Null(x.Node(Isc(2, "cac:TaxCategory/cbc:Percent")));
        // line 3: whisky, system 03, 30 % of (2 × 45.00 × 0.847)
        Assert.Equal("03", x.Text(Isc(3, "cac:TaxCategory/cbc:TierRange")));
        Assert.Equal(76.23m, x.Dec(Isc(3, "cbc:TaxableAmount")));
        Assert.Equal(22.87m, x.Dec(Isc(3, "cbc:TaxAmount")));
        Assert.Equal(59.03m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='2000']/cbc:TaxAmount"));
    }

    [Fact(DisplayName = "AC-11 ICBPER: TaxScheme 7152 with BaseUnitMeasure and PerUnitAmount 0.50, outside the IGV base")]
    public void Ac11_icbper()
    {
        var x = Generate(Examples.Document("issue-invoice-isc.json", 44, "09:30:00"));
        const string bag = "/inv:Invoice/cac:InvoiceLine[4]/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='7152']";
        Assert.Equal(1.50m, x.Dec($"{bag}/cbc:TaxAmount"));
        Assert.Equal(3m, x.Dec($"{bag}/cbc:BaseUnitMeasure"));
        Assert.Equal(0.50m, x.Dec($"{bag}/cac:TaxCategory/cbc:PerUnitAmount"));
        Assert.Equal(0.05m, x.Dec("/inv:Invoice/cac:InvoiceLine[4]/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='1000']/cbc:TaxAmount"));
        Assert.Equal(1.85m, x.Dec("/inv:Invoice/cac:InvoiceLine[4]/cac:TaxTotal/cbc:TaxAmount") + 0.30m);
    }

    [Fact(DisplayName = "AC-65 Dollar invoice: currencyID USD and cac:PaymentExchangeRate 3.745")]
    public void Ac65_usd_exchange_rate()
    {
        var x = Generate(Examples.Document("issue-invoice-usd.json", 45, "16:45:00"));
        Assert.Equal("USD", x.Text("/inv:Invoice/cbc:DocumentCurrencyCode"));
        Assert.Equal("USD", x.Attr("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount", "currencyID"));
        Assert.Equal("USD", x.Text("/inv:Invoice/cac:PaymentExchangeRate/cbc:SourceCurrencyCode"));
        Assert.Equal("PEN", x.Text("/inv:Invoice/cac:PaymentExchangeRate/cbc:TargetCurrencyCode"));
        Assert.Equal(3.745m, x.Dec("/inv:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate"));
        Assert.EndsWith("DOLARES AMERICANOS", x.Text("/inv:Invoice/cbc:Note[@languageLocaleID='1000']"));
    }

    [Fact(DisplayName = "AC-67 Free-of-charge line: price type 02, TaxScheme 9996, legend 1002, not in the payable amount")]
    public void Ac67_free_of_charge_line()
    {
        var x = Generate(Examples.Document("issue-receipt-free-line.json", 10, "18:20:00"));
        Assert.Equal("03", x.Text("/inv:Invoice/cbc:InvoiceTypeCode"));
        const string free = "/inv:Invoice/cac:InvoiceLine[2]";
        Assert.Equal("02", x.Text($"{free}/cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceTypeCode"));
        Assert.Equal(3.00m, x.Dec($"{free}/cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount"));
        Assert.Equal(0.00m, x.Dec($"{free}/cac:Price/cbc:PriceAmount"));
        Assert.Equal("15", x.Text($"{free}/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cbc:TaxExemptionReasonCode"));
        Assert.Equal("9996", x.Text($"{free}/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID"));
        Assert.Equal(6.00m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']/cbc:TaxableAmount"));
        Assert.Equal(1.08m, x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']/cbc:TaxAmount"));
        Assert.NotNull(x.Node("/inv:Invoice/cbc:Note[@languageLocaleID='1002']"));
        Assert.Equal(11.80m, x.Dec("/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"));
    }

    [Fact(DisplayName = "The signature slot exists before signing (ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent)")]
    public void Signature_slot_and_reference()
    {
        var x = Generate(Examples.Cash42());
        Assert.NotNull(x.Node("/inv:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/*"));
        Assert.Equal("#SignABC", x.Text("/inv:Invoice/cac:Signature/cac:DigitalSignatureAttachment/cac:ExternalReference/cbc:URI"));
        Assert.Null(x.Node("//ds:Signature"));
    }

    [Fact(DisplayName = "Special characters are escaped (Arroz & Azúcar <500 g>) and the XML stays valid")]
    public void Special_characters_are_escaped()
    {
        var p = Examples.Products["P-001"] with { Description = "ARROZ & AZÚCAR <500 g> \"EXTRA\"" };
        var src = Examples.Cash42();
        var doc = new ElectronicDocument(src.Type, src.Series, src.Number, src.IssueDate, src.IssueTime, src.Currency, null,
            src.OperationType, src.Issuer, src.Buyer, src.PaymentTerms, [new ItemRequest(p, 1m, "NIU", 5.00m)]);
        var xml = _generator.Generate(doc);
        Assert.Empty(_xsd.Validate(xml));
        Assert.Equal("ARROZ & AZÚCAR <500 g> \"EXTRA\"", new Xml(xml).Text("/inv:Invoice/cac:InvoiceLine/cac:Item/cbc:Description"));
    }

    [Fact(DisplayName = "UTF-8 without BOM, XML declaration, no indentation (a signed XML is never reformatted)")]
    public void Encoding_and_format()
    {
        var xml = _generator.Generate(Examples.Cash42());
        Assert.NotEqual(0xEF, xml[0]);
        var text = System.Text.Encoding.UTF8.GetString(xml);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", text);
        Assert.DoesNotContain("\n", text);
    }

    [Fact(DisplayName = "Generating twice gives the same bytes (deterministic generation)")]
    public void Generation_is_deterministic()
    {
        Assert.Equal(_generator.Generate(Examples.Cash42()), _generator.Generate(Examples.Cash42()));
    }

    [Fact(DisplayName = "The generated XML deserializes back into the generated classes with the same data (round trip)")]
    public void Round_trip_through_the_generated_classes()
    {
        var xml = _generator.Generate(Examples.Cash42());
        var back = Abc.Ubl.UblSerializer.Deserialize<Abc.Ubl.Invoice.InvoiceType>(xml);
        Assert.Equal("F001-42", back.Id.Value);
        Assert.Equal(79.00m, back.LegalMonetaryTotal.PayableAmount.Value);
        Assert.Equal(2, back.InvoiceLine.Count);
        Assert.Equal(xml, Abc.Ubl.UblSerializer.Serialize(back, Ubl21InvoiceGenerator.InvoiceNamespace));
    }
}
