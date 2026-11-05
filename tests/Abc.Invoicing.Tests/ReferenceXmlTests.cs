using Abc.Invoicing.Infrastructure.Ubl;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>
/// The C# generator and the reference XML of the specs (specs/examples/xml, built by tools/ubl_build.py from the
/// same XSD) must say the same thing: same structure and same amounts for the same request.
/// </summary>
public class ReferenceXmlTests
{
    private static readonly string[] Paths =
    [
        "/inv:Invoice/cbc:ID",
        "/inv:Invoice/cbc:IssueDate",
        "/inv:Invoice/cbc:IssueTime",
        "/inv:Invoice/cbc:InvoiceTypeCode",
        "/inv:Invoice/cbc:Note[@languageLocaleID='1000']",
        "/inv:Invoice/cbc:DocumentCurrencyCode",
        "/inv:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID",
        "/inv:Invoice/cac:TaxTotal/cbc:TaxAmount",
        "/inv:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount",
        "/inv:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount",
        "/inv:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount",
    ];

    [Theory(DisplayName = "Same data as the signed reference XML of the specs")]
    [MemberData(nameof(Examples.InvoiceCases), MemberType = typeof(Examples))]
    public void Same_data_as_the_reference_xml(string referenceFile, string request, int number, string time)
    {
        var generated = new Xml(new Ubl21InvoiceGenerator().Generate(Examples.Document(request, number, time)));
        var reference = new Xml(File.ReadAllBytes(Path.Combine(Examples.Folder, "xml", referenceFile)));
        foreach (var p in Paths)
            Assert.Equal(reference.Text(p), generated.Text(p));

        var refLines = reference.Nodes("/inv:Invoice/cac:InvoiceLine");
        var genLines = generated.Nodes("/inv:Invoice/cac:InvoiceLine");
        Assert.Equal(refLines.Count, genLines.Count);
        for (var i = 1; i <= refLines.Count; i++)
            foreach (var part in new[] { "cbc:InvoicedQuantity", "cbc:LineExtensionAmount", "cac:TaxTotal/cbc:TaxAmount",
                         "cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceTypeCode", "cac:Price/cbc:PriceAmount" })
                Assert.Equal(decimal.TryParse(reference.Text($"/inv:Invoice/cac:InvoiceLine[{i}]/{part}"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var r) ? r.ToString() : reference.Text($"/inv:Invoice/cac:InvoiceLine[{i}]/{part}"),
                             decimal.TryParse(generated.Text($"/inv:Invoice/cac:InvoiceLine[{i}]/{part}"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var g) ? g.ToString() : generated.Text($"/inv:Invoice/cac:InvoiceLine[{i}]/{part}"));

        var refSubtotals = reference.Nodes("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID").Select(n => n.InnerText);
        var genSubtotals = generated.Nodes("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:ID").Select(n => n.InnerText);
        Assert.Equal(refSubtotals, genSubtotals);
    }

    [Theory(DisplayName = "The reference XML of the specs is itself valid against the official XSD")]
    [MemberData(nameof(Examples.InvoiceCases), MemberType = typeof(Examples))]
    public void Reference_xml_is_valid(string referenceFile, string request, int number, string time)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Examples.Folder, "xml", referenceFile));
        Assert.Empty(new OasisXsdValidator().Validate(bytes));
        Assert.NotEmpty(request + number + time);
    }
}
