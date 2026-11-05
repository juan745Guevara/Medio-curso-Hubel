namespace Abc.Invoicing.Domain;

/// <summary>Line of a document as the Salesperson enters it: no taxes (the server calculates them, FR-05).</summary>
public sealed record ItemRequest(Product Product, decimal Quantity, string Unit, decimal UnitValue,
    string? IgvTreatment = null, decimal? ReferenceUnitValue = null, decimal Discount = 0m);

/// <summary>A calculated line (FR-05, FR-47, FR-48). All amounts exact decimals (constitution 7).</summary>
public sealed record Line(
    int Order,
    Product Product,
    string IgvTreatment,
    string TaxCode,
    decimal Quantity,
    string Unit,
    decimal UnitValue,
    decimal? ReferenceUnitValue,
    decimal SaleValue,
    decimal IscBase,
    decimal Isc,
    decimal IgvBase,
    decimal Igv,
    int IcbperBags,
    decimal Icbper,
    decimal UnitPrice)
{
    public bool IsFree => Catalogs.FreeOfChargeTreatments.Contains(IgvTreatment);
    public bool HasIsc => Isc != 0m;
    public bool CarriesIgvRate => TaxCode == "1000" || Catalogs.FreeTaxedTreatments.Contains(IgvTreatment);

    /// <summary>cac:InvoiceLine/cac:TaxTotal/cbc:TaxAmount: taxes charged on the line (free lines report the IGV, not charged).</summary>
    public decimal LineTaxAmount => IsFree ? Igv : Isc + Igv + Icbper;
}

/// <summary>Document totals (FR-06).</summary>
public sealed record Totals(
    decimal Taxed, decimal Exempt, decimal NonTaxable, decimal FreeValue, decimal FreeIgv,
    decimal Igv, decimal IscBase, decimal Isc, decimal Icbper, decimal LineExtension, decimal TaxInclusive, decimal Payable)
{
    public decimal TaxTotal => Igv + Isc + Icbper;
}

/// <summary>
/// Pure domain service (domain-model.md): sale value, ISC (catalog 08) before IGV, IGV on sale value + ISC,
/// ICBPER per bag outside the IGV base, free-of-charge lines outside the payable amount.
/// Same rules as tools/sunat_calc.py, so the C# generator and the reference XML of the specs agree.
/// </summary>
public static class TaxCalculator
{
    public const decimal IgvRate = 0.18m;          // 15.5 % IGV + 2.5 % IPM, shown as 18 (research 001)
    public const decimal IcbperRate = 0.50m;       // per bag since 2023 (research 001)
    public const decimal RetailPriceFactor = 0.847m; // 1 / 1.18, 3 decimals (ISC system 03)

    /// <summary>Half-up to 2 decimals and always with scale 2, so XmlSerializer writes "50.00", not "50".</summary>
    public static decimal R2(decimal x) => decimal.Round(x, 2, MidpointRounding.AwayFromZero) + 0.00m;

    public static Line CalculateLine(int order, ItemRequest it)
    {
        var p = it.Product;
        var treatment = it.IgvTreatment ?? p.IgvTreatment;
        var free = Catalogs.FreeOfChargeTreatments.Contains(treatment);
        var saleValue = free ? R2(it.Quantity * (it.ReferenceUnitValue ?? 0m)) : R2(it.Quantity * it.UnitValue - it.Discount);
        var taxCode = free ? "9996" : treatment[0] switch
        {
            '1' => "1000",
            '2' => "9997",
            '3' => "9998",
            _ => "9995",
        };

        decimal iscBase = 0m, isc = 0m;
        if (p.Isc is { } cfg && !free)
        {
            cfg.Validate();
            switch (cfg.System)
            {
                case IscSystem.AdValorem:
                    iscBase = saleValue;
                    isc = R2(saleValue * cfg.Rate!.Value / 100m);
                    break;
                case IscSystem.FixedAmount: // FR-48: quantity in the unit of the tax (e.g. litres per bottle)
                    iscBase = saleValue;
                    isc = R2(it.Quantity * p.IscUnitsPerItem * cfg.FixedAmount!.Value);
                    break;
                case IscSystem.RetailPrice:
                    iscBase = R2(it.Quantity * cfg.SuggestedRetailPrice!.Value * RetailPriceFactor);
                    isc = R2(iscBase * cfg.Rate!.Value / 100m);
                    break;
            }
        }

        var carriesIgv = taxCode == "1000" || Catalogs.FreeTaxedTreatments.Contains(treatment);
        var igvBase = carriesIgv ? saleValue + isc : saleValue;
        var igv = carriesIgv ? R2(igvBase * IgvRate) : 0.00m;

        var bags = p.IsPlasticBag && !free ? (int)it.Quantity : 0;
        var icbper = R2(bags * IcbperRate);
        var unitPrice = free ? 0m : (saleValue + isc + igv) / it.Quantity;

        return new Line(order, p, treatment, taxCode, it.Quantity, it.Unit, it.UnitValue, it.ReferenceUnitValue,
            saleValue, iscBase, isc, igvBase, igv, bags, icbper, unitPrice);
    }

    public static Totals Calculate(IReadOnlyList<Line> lines)
    {
        decimal taxed = 0, exempt = 0, nonTaxable = 0, freeValue = 0, freeIgv = 0, igv = 0, iscBase = 0, isc = 0, icbper = 0, lineExt = 0;
        foreach (var l in lines)
        {
            if (l.IsFree)
            {
                freeValue += l.SaleValue;
                freeIgv += l.Igv;
                continue;
            }
            lineExt += l.SaleValue;
            switch (l.TaxCode)
            {
                case "1000": taxed += l.SaleValue; igv += l.Igv; break;
                case "9997": exempt += l.SaleValue; break;
                case "9998": nonTaxable += l.SaleValue; break;
            }
            if (l.HasIsc) { isc += l.Isc; iscBase += l.IscBase; }
            icbper += l.Icbper;
        }
        var inclusive = lineExt + igv + isc + icbper;
        return new Totals(R2(taxed), R2(exempt), R2(nonTaxable), R2(freeValue), R2(freeIgv), R2(igv), R2(iscBase), R2(isc),
            R2(icbper), R2(lineExt), R2(inclusive), R2(inclusive));
    }
}
