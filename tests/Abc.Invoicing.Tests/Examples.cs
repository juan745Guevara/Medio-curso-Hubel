using System.Globalization;
using System.Text.Json;
using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Tests;

/// <summary>Builds domain documents from the JSON examples of specs/examples/valid (the API contract).</summary>
public static class Examples
{
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "examples");

    private static JsonElement Read(string relative) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, relative))).RootElement;

    private static decimal Dec(JsonElement e) => decimal.Parse(e.GetString()!, CultureInfo.InvariantCulture);

    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) ? v.GetString() : null;

    public static IReadOnlyDictionary<string, Product> Products { get; } = LoadProducts();

    private static Dictionary<string, Product> LoadProducts()
    {
        var map = new Dictionary<string, Product>();
        foreach (var p in Read("valid/products.json").EnumerateArray())
        {
            IscConfig? isc = null;
            if (p.TryGetProperty("isc", out var i))
                isc = new IscConfig(
                    (IscSystem)int.Parse(i.GetProperty("system").GetString()!),
                    i.TryGetProperty("rate", out var r) ? Dec(r) : null,
                    i.TryGetProperty("fixedAmount", out var f) ? Dec(f) : null,
                    i.TryGetProperty("suggestedRetailPrice", out var s) ? Dec(s) : null);
            map[p.GetProperty("code").GetString()!] = new Product(
                p.GetProperty("code").GetString()!, p.GetProperty("description").GetString()!, p.GetProperty("unit").GetString()!,
                p.GetProperty("igvTreatment").GetString()!, isc,
                p.TryGetProperty("iscUnitsPerItem", out var u) ? Dec(u) : 1m,
                p.TryGetProperty("isPlasticBag", out var b) && b.GetBoolean(),
                Str(p, "unspscCode"));
        }
        return map;
    }

    public static Issuer Issuer()
    {
        var e = Read("xml/issuer.json");
        var a = e.GetProperty("address");
        return new Issuer(new Ruc(e.GetProperty("ruc").GetString()!), e.GetProperty("name").GetString()!,
            e.GetProperty("tradeName").GetString()!, e.GetProperty("establishmentCode").GetString()!,
            new Address(a.GetProperty("ubigeo").GetString()!, a.GetProperty("department").GetString()!,
                a.GetProperty("province").GetString()!, a.GetProperty("district").GetString()!, a.GetProperty("line").GetString()!));
    }

    public static IReadOnlyList<ItemRequest> Items(JsonElement items) => items.EnumerateArray().Select(it => new ItemRequest(
        Products[it.GetProperty("productCode").GetString()!], Dec(it.GetProperty("quantity")), it.GetProperty("unit").GetString()!,
        Dec(it.GetProperty("unitValue")), Str(it, "igvTreatment"),
        it.TryGetProperty("referenceUnitValue", out var rv) ? Dec(rv) : null,
        it.TryGetProperty("discount", out var d) ? Dec(d) : 0m)).ToList();

    /// <summary>An IssueDocumentRequest example, numbered by the server as in specs/examples/xml/cases.json.</summary>
    public static ElectronicDocument Document(string requestFile, int number, string issueTime)
    {
        var r = Read("valid/" + requestFile);
        var b = r.GetProperty("buyer");
        var doc = b.GetProperty("document");
        var pt = r.GetProperty("paymentTerms");
        var terms = pt.GetProperty("type").GetString() == "CASH"
            ? PaymentTerms.Cash()
            : PaymentTerms.Credit(Dec(pt.GetProperty("outstandingAmount")),
                pt.GetProperty("installments").EnumerateArray()
                    .Select(q => new Installment(Dec(q.GetProperty("amount")), DateOnly.Parse(q.GetProperty("dueDate").GetString()!, CultureInfo.InvariantCulture)))
                    .ToArray());
        return new ElectronicDocument(
            r.GetProperty("type").GetString() == "01" ? DocumentType.Invoice : DocumentType.SalesReceipt,
            r.GetProperty("series").GetString()!, number,
            DateOnly.Parse(r.GetProperty("issueDate").GetString()!, CultureInfo.InvariantCulture),
            TimeOnly.Parse(issueTime, CultureInfo.InvariantCulture),
            r.GetProperty("currency").GetString()!,
            r.TryGetProperty("exchangeRate", out var x) ? Dec(x) : null,
            r.GetProperty("operationType").GetString()!, Issuer(),
            new Buyer(doc.GetProperty("type").GetString()!, doc.GetProperty("number").GetString()!, b.GetProperty("name").GetString()!, Str(b, "address")),
            terms, Items(r.GetProperty("items")));
    }

    /// <summary>The five invoice/receipt cases of specs/examples/xml/cases.json: (reference file, request, number, time).</summary>
    public static IEnumerable<object[]> InvoiceCases()
    {
        var cases = Read("xml/cases.json");
        foreach (var c in cases.EnumerateObject())
            if (c.Value.GetProperty("kind").GetString() == "invoice")
                yield return
                [
                    c.Name, c.Value.GetProperty("request").GetString()!.Replace("valid/", ""),
                    c.Value.GetProperty("number").GetInt32(), c.Value.GetProperty("issueTime").GetString()!,
                ];
    }

    public static ElectronicDocument Cash42() => Document("issue-invoice-cash.json", 42, "10:15:00");
}
