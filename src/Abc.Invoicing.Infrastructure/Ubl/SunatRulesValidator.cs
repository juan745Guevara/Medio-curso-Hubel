using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Infrastructure.Ubl;

/// <summary>
/// Adapter of <see cref="ISunatRulesValidator"/> (FR-52): SUNAT's validation rules that the OASIS XSD cannot express,
/// checked on the XML itself — the same document SUNAT will receive — before signing. Each rule cites the row of the
/// regulatory matrix and returns SUNAT's own code and text (sunat-return-codes.json; texts from SUNAT's return-code
/// catalog as kept by Greenter's CodeErrors.xml). Codes 2000–3999 would be a rejection, 4000+ an observation.
/// Amount checks allow SUNAT's rounding tolerance of ±1.00.
/// </summary>
public sealed class SunatRulesValidator : ISunatRulesValidator
{
    private const decimal Tolerance = 1.00m;
    private const string InvNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private const string CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    /// <summary>Catalog 51 (operation type) as versioned in specs/catalogs, copied next to the binaries.</summary>
    private static readonly Lazy<HashSet<string>> OperationTypes = new(() =>
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "catalogs", "catalog-51.json")));
        return json.RootElement.GetProperty("codes").EnumerateArray().Select(c => c.GetProperty("code").GetString()!).ToHashSet();
    });

    public IReadOnlyList<RuleViolation> Validate(byte[] xml)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.Load(new MemoryStream(xml));
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("inv", InvNs);
        ns.AddNamespace("cac", CacNs);
        ns.AddNamespace("cbc", CbcNs);
        ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
        var x = new Reader(doc, ns);
        var v = new List<RuleViolation>();
        void Fail(string code, string message) => v.Add(new RuleViolation(code, message));

        // Header (matrix rows 3, 4, 5, 9)
        if (x.Text("/inv:Invoice/cbc:UBLVersionID") != "2.1")
            Fail("2074", "UBLVersionID - La versión del UBL no es correcta");
        if (x.Text("/inv:Invoice/cbc:CustomizationID") != "2.0")
            Fail("2072", "CustomizationID - La versión del documento no es la correcta");
        var type = x.Text("/inv:Invoice/cbc:InvoiceTypeCode");
        var id = x.Text("/inv:Invoice/cbc:ID") ?? "";
        var letter = type == DocumentType.Invoice.Code() ? "F" : type == DocumentType.SalesReceipt.Code() ? "B" : null;
        if (letter is null || !Regex.IsMatch(id, $"^{letter}[A-Z0-9]{{3}}-[1-9][0-9]{{0,7}}$"))
            Fail("1001", "ID - El dato SERIE-CORRELATIVO no cumple con el formato de acuerdo al tipo de comprobante");
        var operation = x.Attr("/inv:Invoice/cbc:InvoiceTypeCode", "listID");
        if (string.IsNullOrEmpty(operation))
            Fail("3205", "Debe consignar el tipo de operación");
        else if (!OperationTypes.Value.Contains(operation))
            Fail("3206", "El dato ingresado como tipo de operación no corresponde a un valor esperado (catálogo nro. 51)");

        // Currency (rows 11 and 50)
        var currency = x.Text("/inv:Invoice/cbc:DocumentCurrencyCode");
        if (doc.SelectNodes("//@currencyID")!.OfType<XmlAttribute>().Any(a => a.Value != currency))
            Fail("2071", "La moneda debe ser la misma en todo el documento. Salvo las percepciones que sólo son en moneda nacional");
        if (currency == "USD")
        {
            var rate = x.Dec("/inv:Invoice/cac:PaymentExchangeRate/cbc:CalculationRate");
            if (rate is null)
                Fail("2721", "El XML no contiene el tag o no existe información del tipo de cambio");
            else if (rate <= 0)
                Fail("2716", "El dato ingresado en el tipo de cambio debe ser numérico mayor a cero");
        }

        // Buyer (rows 17 and 18)
        const string buyerId = "/inv:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID";
        var buyerType = x.Attr(buyerId, "schemeID");
        if (type == DocumentType.Invoice.Code() && buyerType != "6")
            Fail("2800", "El dato ingresado en el tipo de documento de identidad del receptor no esta permitido.");
        if (buyerType == "6" && !Ruc.IsValid(x.Text(buyerId)))
            Fail("2802", "El dato ingresado como numero de documento de identidad del receptor no cumple con el formato establecido");

        // Legends (rows 10 and 54)
        var legends = x.Nodes("/inv:Invoice/cbc:Note").Select(n => n.GetAttribute("languageLocaleID")).Where(c => c.Length > 0).ToList();
        if (legends.Count != legends.Distinct().Count())
            Fail("3014", "El codigo de leyenda no debe repetirse en el comprobante.");

        // Lines (rows 23 to 36, 51, 52)
        var lines = x.Nodes("/inv:Invoice/cac:InvoiceLine");
        var lineIds = lines.Select(l => x.Text("cbc:ID", l)).ToList();
        if (lineIds.Count != lineIds.Distinct().Count())
            Fail("2752", "El número de ítem no puede estar duplicado.");
        decimal sumOnerousLines = 0;
        var hasFreeLine = false;
        foreach (var line in lines)
        {
            var lineNo = x.Text("cbc:ID", line);
            var qty = x.Dec("cbc:InvoicedQuantity", line) ?? 0;
            if (qty <= 0)
                Fail("2024", $"El XML no contiene el tag InvoicedQuantity en el detalle de los Items o es cero (0) (ítem {lineNo})");
            var free = x.Text("cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceTypeCode", line) == "02";
            hasFreeLine |= free;
            var lineExtension = x.Dec("cbc:LineExtensionAmount", line) ?? 0;
            var unit = free
                ? x.Dec("cac:PricingReference/cac:AlternativeConditionPrice/cbc:PriceAmount", line) ?? 0
                : x.Dec("cac:Price/cbc:PriceAmount", line) ?? 0;
            var discounts = x.Nodes("cac:AllowanceCharge[cbc:ChargeIndicator='false']/cbc:Amount", line).Sum(n => Dec(n.InnerText));
            if (Math.Abs(qty * unit - discounts - lineExtension) > Tolerance)
                Fail("4288", $"El valor de venta por ítem difiere de los importes consignados. (ítem {lineNo})");
            if (!free)
                sumOnerousLines += lineExtension;
            foreach (var sub in x.Nodes("cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='1000' or cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']", line))
            {
                var percent = x.Dec("cac:TaxCategory/cbc:Percent", sub) ?? 0;
                var expected = (x.Dec("cbc:TaxableAmount", sub) ?? 0) * percent / 100m;
                if (percent > 0 && x.Text("cac:TaxCategory/cbc:TaxExemptionReasonCode", sub) is { } t && (t == "10" || Catalogs.FreeTaxedTreatments.Contains(t))
                    && Math.Abs(expected - (x.Dec("cbc:TaxAmount", sub) ?? 0)) > Tolerance)
                    Fail("4290", $"El cálculo del IGV es Incorrecto (ítem {lineNo})");
            }
        }

        // Free-of-charge total (rows 53 and 54)
        var freeTotal = x.Dec("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID='9996']/cbc:TaxableAmount");
        if (hasFreeLine && !(freeTotal > 0))
            Fail("2641", "Operacion gratuita,  debe consignar Total valor venta - operaciones gratuitas  mayor a cero");
        if (legends.Contains("1002") && freeTotal is null)
            Fail("2416", "Si existe leyenda Transferencia Gratuita debe consignar Total Valor de Venta de Operaciones Gratuitas");

        // Totals (rows 37 to 45)
        var headerTax = x.Dec("/inv:Invoice/cac:TaxTotal/cbc:TaxAmount") ?? 0;
        var sumSubtotals = x.Nodes("/inv:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:ID!='9996']/cbc:TaxAmount").Sum(n => Dec(n.InnerText));
        if (Math.Abs(headerTax - sumSubtotals) > Tolerance)
            Fail("4301", "La sumatoria de impuestos globales no corresponde al monto total de impuestos.");
        const string total = "/inv:Invoice/cac:LegalMonetaryTotal/";
        var lineExtTotal = x.Dec(total + "cbc:LineExtensionAmount") ?? 0;
        if (Math.Abs(lineExtTotal - sumOnerousLines) > Tolerance)
            Fail("4309", "La sumatoria de valor de venta no corresponde a los importes consignados");
        var taxInclusive = x.Dec(total + "cbc:TaxInclusiveAmount") ?? 0;
        if (Math.Abs(lineExtTotal + headerTax - taxInclusive) > Tolerance)
            Fail("4211", "El importe del campo /cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount no coincide con el valor calculado");
        var payable = x.Dec(total + "cbc:PayableAmount") ?? 0;
        var expectedPayable = taxInclusive - (x.Dec(total + "cbc:AllowanceTotalAmount") ?? 0) + (x.Dec(total + "cbc:ChargeTotalAmount") ?? 0)
            - (x.Dec(total + "cbc:PrepaidAmount") ?? 0) + (x.Dec(total + "cbc:PayableRoundingAmount") ?? 0);
        if (Math.Abs(expectedPayable - payable) > Tolerance)
            Fail("4312", "El importe total del comprobante no coincide con el valor calculado");

        // Payment terms, invoices only (rows 20 to 22)
        if (type == DocumentType.Invoice.Code())
        {
            var terms = x.Nodes("/inv:Invoice/cac:PaymentTerms[cbc:ID='FormaPago']");
            var means = terms.Select(t => x.Text("cbc:PaymentMeansID", t)).ToList();
            if (!means.Contains("Contado") && !means.Contains("Credito"))
                Fail("3244", "Debe consignar la informacion del tipo de transaccion del comprobante");
            if (means.Contains("Credito"))
            {
                var credit = terms.First(t => x.Text("cbc:PaymentMeansID", t) == "Credito");
                var outstanding = x.Dec("cbc:Amount", credit);
                if (outstanding is null)
                    Fail("3251", "Si el tipo de transaccion es al Credito debe consignarse el Monto neto pendiente de pago");
                else if (outstanding > payable)
                    Fail("3265", "El Monto neto pendiente de pago debe ser menor o igual al Importe total del comprobante");
                var installments = terms.Where(t => x.Text("cbc:PaymentMeansID", t)?.StartsWith("Cuota", StringComparison.Ordinal) == true).ToList();
                if (installments.Count == 0)
                    Fail("3249", "Si el tipo de transaccion es al Credito debe existir al menos información de una cuota de pago");
                if (installments.Any(q => (x.Dec("cbc:Amount", q) ?? 0) > payable))
                    Fail("3266", "El Monto del pago único o de las cuotas debe ser menor o igual al Importe total del comprobante");
                var issueDate = x.Text("/inv:Invoice/cbc:IssueDate");
                if (installments.Any(q => string.CompareOrdinal(x.Text("cbc:PaymentDueDate", q), issueDate) < 0))
                    Fail("3267", "Fecha del pago único o de las cuotas no puede ser anterior a la fecha de emisión del comprobante");
            }
        }

        // Signature reference (row 2)
        if (string.IsNullOrWhiteSpace(x.Text("/inv:Invoice/cac:Signature/cbc:ID")))
            Fail("2076", "cac:Signature/cbc:ID - Falta el identificador de la firma");

        return v;
    }

    private static decimal Dec(string s) => decimal.Parse(s.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture);

    private sealed class Reader(XmlDocument doc, XmlNamespaceManager ns)
    {
        public string? Text(string xpath, XmlNode? context = null) => (context ?? doc).SelectSingleNode(xpath, ns)?.InnerText.Trim();

        public string? Attr(string xpath, string name) => ((XmlElement?)doc.SelectSingleNode(xpath, ns))?.GetAttribute(name);

        public decimal? Dec(string xpath, XmlNode? context = null) =>
            Text(xpath, context) is { Length: > 0 } s && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

        public IReadOnlyList<XmlElement> Nodes(string xpath, XmlNode? context = null) =>
            (context ?? doc).SelectNodes(xpath, ns)!.OfType<XmlElement>().ToList();
    }
}
