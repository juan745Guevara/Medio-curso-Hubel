namespace Abc.Invoicing.Domain;

/// <summary>ISC configuration of a Product (catalog 08). Rate for systems 01 and 03; fixed amount for 02.</summary>
public sealed record IscConfig(IscSystem System, decimal? Rate = null, decimal? FixedAmount = null, decimal? SuggestedRetailPrice = null)
{
    public void Validate()
    {
        var ok = System switch
        {
            IscSystem.AdValorem => Rate is > 0 && FixedAmount is null,
            IscSystem.FixedAmount => FixedAmount is > 0 && Rate is null,
            IscSystem.RetailPrice => Rate is > 0 && SuggestedRetailPrice is > 0 && FixedAmount is null,
            _ => false,
        };
        if (!ok)
            throw new DomainException("INVALID_ISC_SYSTEM", $"ISC system {System.Code()} configured incompletely");
    }
}

/// <summary>Product of the Catalog context (domain-model.md).</summary>
public sealed record Product(
    string Code,
    string Description,
    string Unit,
    string IgvTreatment,
    IscConfig? Isc = null,
    decimal IscUnitsPerItem = 1m,
    bool IsPlasticBag = false,
    string? UnspscCode = null);
