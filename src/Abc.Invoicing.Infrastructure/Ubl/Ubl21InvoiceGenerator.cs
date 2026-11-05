using System.Xml;
using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;
using Abc.Ubl;
using Cac = Abc.Ubl.Cac;
using Cbc = Abc.Ubl.Cbc;
using Ext = Abc.Ubl.Ext;
using UblInvoice = Abc.Ubl.Invoice.InvoiceType;

namespace Abc.Invoicing.Infrastructure.Ubl;

/// <summary>
/// Adapter of <see cref="IUblGenerator"/> (ADR 0005): fills the classes generated from UBL-Invoice-2.1.xsd with the
/// SUNAT customization of the regulatory matrix (rows 1–54) and serializes them with XmlSerializer.
/// No templates and no string concatenation: element order and cardinality come from the generated classes.
/// </summary>
public sealed class Ubl21InvoiceGenerator : IUblGenerator
{
    public const string InvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    /// <summary>
    /// ext:ExtensionContent must hold exactly one element, and the ds:Signature does not exist before signing.
    /// This neutral element (a namespace the XSD skips) keeps the unsigned XML valid; the signer replaces it (ADR 0003).
    /// </summary>
    public const string PlaceholderNamespace = "urn:abc:signature-placeholder";
    public const string PlaceholderName = "SignaturePlaceholder";

    private const string SunatAgency = "PE:SUNAT";
    private const string Unece = "United Nations Economic Commission for Europe";
    private const string CatalogUri = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo";

    public byte[] Generate(ElectronicDocument doc) => UblSerializer.Serialize(Map(doc), InvoiceNamespace);

    public static UblInvoice Map(ElectronicDocument doc)
    {
        var cur = doc.Currency;
        var t = doc.Totals;
        var inv = new UblInvoice
        {
            UblExtensions = [new Ext.UblExtensionType { ExtensionContent = new Ext.ExtensionContentType { Any = Placeholder() } }],
            UblVersionId = new Cbc.UblVersionIdType { Value = "2.1" },                                        // row 3
            CustomizationId = new Cbc.CustomizationIdType { Value = "2.0", SchemeAgencyName = SunatAgency },  // row 4
            Id = new Cbc.IdType { Value = doc.Id },                                                           // row 5
            IssueDate = new Cbc.IssueDateType { Value = doc.IssueDate.ToDateTime(TimeOnly.MinValue) },        // row 6
            IssueTime = new Cbc.IssueTimeType { Value = DateTime.MinValue.Add(doc.IssueTime.ToTimeSpan()) },  // row 7
            InvoiceTypeCode = new Cbc.InvoiceTypeCodeType                                                     // rows 8–9
            {
                Value = doc.Type.Code(), ListId = doc.OperationType, ListAgencyName = SunatAgency, ListName = "Tipo de Documento",
                Name = "Tipo de Operacion", ListUri = CatalogUri + "01", ListSchemeUri = CatalogUri + "51",
            },
            Note = Legends(doc),                                                                               // rows 10, 54
            DocumentCurrencyCode = new Cbc.DocumentCurrencyCodeType                                            // row 11
            {
                Value = cur, ListId = "ISO 4217 Alpha", ListName = "Currency", ListAgencyName = Unece,
            },
            Signature = [SignatureReference(doc.Issuer)],                                                      // row 2
            AccountingSupplierParty = Supplier(doc.Issuer),                                                    // rows 13–16
            AccountingCustomerParty = Customer(doc.Buyer),                                                     // rows 17–19
            PaymentTerms = Payment(doc.PaymentTerms, cur),                                                     // rows 20–22
            TaxTotal = [HeaderTaxTotal(t, cur)],                                                               // rows 37–41, 53
            LegalMonetaryTotal = new Cac.MonetaryTotalType                                                     // rows 42–45
            {
                LineExtensionAmount = Amount<Cbc.LineExtensionAmountType>(t.LineExtension, cur),
                TaxInclusiveAmount = Amount<Cbc.TaxInclusiveAmountType>(t.TaxInclusive, cur),
                PayableAmount = Amount<Cbc.PayableAmountType>(t.Payable, cur),
            },
            InvoiceLine = doc.Lines.Select(l => Line(l, cur)).ToList(),                                        // rows 23–36, 51–52
        };
        if (doc.PaymentTerms.IsCredit)                                                                         // row 12
            inv.DueDate = new Cbc.DueDateType { Value = doc.PaymentTerms.Installments[^1].DueDate.ToDateTime(TimeOnly.MinValue) };
        if (cur == "USD")                                                                                      // row 50
            inv.PaymentExchangeRate = new Cac.ExchangeRateType
            {
                SourceCurrencyCode = new Cbc.SourceCurrencyCodeType { Value = "USD" },
                TargetCurrencyCode = new Cbc.TargetCurrencyCodeType { Value = "PEN" },
                CalculationRate = new Cbc.CalculationRateType { Value = doc.ExchangeRate!.Value },
                Date = new Cbc.DateType_2 { Value = doc.IssueDate.ToDateTime(TimeOnly.MinValue) },
            };
        return inv;
    }

    private static XmlElement Placeholder() => new XmlDocument().CreateElement(PlaceholderName, PlaceholderNamespace);

    private static List<Cbc.NoteType> Legends(ElectronicDocument doc)
    {
        var notes = new List<Cbc.NoteType> { new() { Value = AmountInWords.Of(doc.Totals.Payable, doc.Currency), LanguageLocaleId = "1000" } };
        if (doc.Totals.FreeValue > 0)
            notes.Add(new Cbc.NoteType { Value = "TRANSFERENCIA GRATUITA DE UN BIEN Y/O SERVICIO PRESTADO GRATUITAMENTE", LanguageLocaleId = "1002" });
        return notes;
    }

    private static Cac.SignatureType SignatureReference(Issuer issuer) => new()
    {
        Id = new Cbc.IdType { Value = "IDSignABC" },
        SignatoryParty = new Cac.PartyType
        {
            PartyIdentification = [new Cac.PartyIdentificationType { Id = new Cbc.IdType { Value = issuer.Ruc.Value } }],
            PartyName = [new Cac.PartyNameType { Name = new Cbc.NameType_2 { Value = issuer.BusinessName } }],
        },
        DigitalSignatureAttachment = new Cac.AttachmentType
        {
            ExternalReference = new Cac.ExternalReferenceType { Uri = new Cbc.UriType { Value = "#SignABC" } },
        },
    };

    private static Cbc.IdType IdentityId(string type, string number) => new()
    {
        Value = number, SchemeId = type, SchemeName = "Documento de Identidad", SchemeAgencyName = SunatAgency, SchemeUri = CatalogUri + "06",
    };

    private static Cac.SupplierPartyType Supplier(Issuer issuer)
    {
        var a = issuer.Address;
        return new Cac.SupplierPartyType
        {
            Party = new Cac.PartyType
            {
                PartyIdentification = [new Cac.PartyIdentificationType { Id = IdentityId("6", issuer.Ruc.Value) }],
                PartyName = [new Cac.PartyNameType { Name = new Cbc.NameType_2 { Value = issuer.TradeName } }],
                PartyLegalEntity =
                [
                    new Cac.PartyLegalEntityType
                    {
                        RegistrationName = new Cbc.RegistrationNameType { Value = issuer.BusinessName },
                        RegistrationAddress = new Cac.AddressType
                        {
                            Id = new Cbc.IdType { Value = a.Ubigeo, SchemeAgencyName = "PE:INEI", SchemeName = "Ubigeos" },
                            AddressTypeCode = new Cbc.AddressTypeCodeType
                            {
                                Value = issuer.EstablishmentCode, ListAgencyName = SunatAgency, ListName = "Establecimientos anexos",
                            },
                            CityName = new Cbc.CityNameType { Value = a.Province },
                            CountrySubentity = new Cbc.CountrySubentityType { Value = a.Department },
                            District = new Cbc.DistrictType { Value = a.District },
                            AddressLine = [new Cac.AddressLineType { Line = new Cbc.LineType { Value = a.Line } }],
                            Country = new Cac.CountryType
                            {
                                IdentificationCode = new Cbc.IdentificationCodeType
                                {
                                    Value = "PE", ListId = "ISO 3166-1", ListAgencyName = Unece, ListName = "Country",
                                },
                            },
                        },
                    },
                ],
            },
        };
    }

    private static Cac.CustomerPartyType Customer(Buyer buyer)
    {
        var entity = new Cac.PartyLegalEntityType { RegistrationName = new Cbc.RegistrationNameType { Value = buyer.Name } };
        if (!string.IsNullOrEmpty(buyer.Address))
            entity.RegistrationAddress = new Cac.AddressType { AddressLine = [new Cac.AddressLineType { Line = new Cbc.LineType { Value = buyer.Address } }] };
        return new Cac.CustomerPartyType
        {
            Party = new Cac.PartyType
            {
                PartyIdentification = [new Cac.PartyIdentificationType { Id = IdentityId(buyer.DocumentType, buyer.DocumentNumber) }],
                PartyLegalEntity = [entity],
            },
        };
    }

    /// <summary>RS 193-2020: literal values FormaPago / Contado / Credito / Cuota001… (matrix §C).</summary>
    private static List<Cac.PaymentTermsType> Payment(PaymentTerms pt, string cur)
    {
        if (!pt.IsCredit)
            return [new Cac.PaymentTermsType { Id = new Cbc.IdType { Value = "FormaPago" }, PaymentMeansId = [new Cbc.PaymentMeansIdType { Value = "Contado" }] }];
        var terms = new List<Cac.PaymentTermsType>
        {
            new()
            {
                Id = new Cbc.IdType { Value = "FormaPago" }, PaymentMeansId = [new Cbc.PaymentMeansIdType { Value = "Credito" }],
                Amount = Amount<Cbc.AmountType_3>(pt.OutstandingAmount, cur),
            },
        };
        terms.AddRange(pt.Installments.Select((q, i) => new Cac.PaymentTermsType
        {
            Id = new Cbc.IdType { Value = "FormaPago" },
            PaymentMeansId = [new Cbc.PaymentMeansIdType { Value = $"Cuota{i + 1:000}" }],
            Amount = Amount<Cbc.AmountType_3>(q.Amount, cur),
            PaymentDueDate = new Cbc.PaymentDueDateType { Value = q.DueDate.ToDateTime(TimeOnly.MinValue) },
        }));
        return terms;
    }

    private static Cac.TaxSchemeType Scheme(string code05)
    {
        var (code, name, type) = Catalogs.TaxScheme(code05);
        return new Cac.TaxSchemeType
        {
            Id = new Cbc.IdType { Value = code, SchemeName = "Codigo de tributos", SchemeAgencyName = SunatAgency, SchemeUri = CatalogUri + "05" },
            Name = new Cbc.NameType_2 { Value = name },
            TaxTypeCode = new Cbc.TaxTypeCodeType { Value = type },
        };
    }

    private static Cac.TaxSubtotalType Subtotal(decimal? taxable, decimal tax, string code05, string cur) => new()
    {
        TaxableAmount = taxable is null ? null : Amount<Cbc.TaxableAmountType>(taxable.Value, cur),
        TaxAmount = Amount<Cbc.TaxAmountType>(tax, cur),
        TaxCategory = new Cac.TaxCategoryType { TaxScheme = Scheme(code05) },
    };

    private static Cac.TaxTotalType HeaderTaxTotal(Totals t, string cur)
    {
        var subs = new List<Cac.TaxSubtotalType>();
        if (t.Taxed != 0 || t.Igv != 0) subs.Add(Subtotal(t.Taxed, t.Igv, "1000", cur));
        if (t.Exempt != 0) subs.Add(Subtotal(t.Exempt, 0m, "9997", cur));
        if (t.NonTaxable != 0) subs.Add(Subtotal(t.NonTaxable, 0m, "9998", cur));
        if (t.FreeValue != 0) subs.Add(Subtotal(t.FreeValue, t.FreeIgv, "9996", cur));
        if (t.Isc != 0) subs.Add(Subtotal(t.IscBase, t.Isc, "2000", cur));
        if (t.Icbper != 0) subs.Add(Subtotal(null, t.Icbper, "7152", cur));
        return new Cac.TaxTotalType { TaxAmount = Amount<Cbc.TaxAmountType>(t.TaxTotal, cur), TaxSubtotal = subs };
    }

    private static Cac.InvoiceLineType Line(Domain.Line l, string cur)
    {
        var subs = new List<Cac.TaxSubtotalType>();
        if (l.HasIsc)                                                                                 // rows 33–34: catalog 08 in TierRange
        {
            var isc = l.Product.Isc!;
            subs.Add(new Cac.TaxSubtotalType
            {
                TaxableAmount = Amount<Cbc.TaxableAmountType>(l.IscBase, cur),
                TaxAmount = Amount<Cbc.TaxAmountType>(l.Isc, cur),
                TaxCategory = new Cac.TaxCategoryType
                {
                    Percent = isc.Rate is { } rate ? new Cbc.PercentType_2 { Value = Fmt(rate) } : null,
                    TierRange = new Cbc.TierRangeType { Value = isc.System.Code() },
                    TaxScheme = Scheme("2000"),
                },
            });
        }
        subs.Add(new Cac.TaxSubtotalType                                                              // rows 31–32, 36, 52
        {
            TaxableAmount = Amount<Cbc.TaxableAmountType>(l.IgvBase, cur),
            TaxAmount = Amount<Cbc.TaxAmountType>(l.Igv, cur),
            TaxCategory = new Cac.TaxCategoryType
            {
                Percent = new Cbc.PercentType_2 { Value = l.CarriesIgvRate ? 18.00m : 0.00m },
                TaxExemptionReasonCode = new Cbc.TaxExemptionReasonCodeType
                {
                    Value = l.IgvTreatment, ListAgencyName = SunatAgency, ListName = "Afectacion del IGV", ListUri = CatalogUri + "07",
                },
                TaxScheme = Scheme(l.TaxCode),
            },
        });
        if (l.Icbper != 0)                                                                            // row 35
            subs.Add(new Cac.TaxSubtotalType
            {
                TaxAmount = Amount<Cbc.TaxAmountType>(l.Icbper, cur),
                BaseUnitMeasure = new Cbc.BaseUnitMeasureType { Value = l.IcbperBags, UnitCode = "NIU" },
                TaxCategory = new Cac.TaxCategoryType
                {
                    PerUnitAmount = Amount<Cbc.PerUnitAmountType>(TaxCalculator.IcbperRate, cur),
                    TaxScheme = Scheme("7152"),
                },
            });

        var price = l.IsFree                                                                          // rows 26, 51
            ? new Cac.PriceType { PriceAmount = Amount<Cbc.PriceAmountType>(l.ReferenceUnitValue!.Value, cur), PriceTypeCode = PriceType("02") }
            : new Cac.PriceType { PriceAmount = new Cbc.PriceAmountType { Value = Fmt(l.UnitPrice), CurrencyId = cur }, PriceTypeCode = PriceType("01") };

        var item = new Cac.ItemType                                                                   // rows 28–30
        {
            Description = [new Cbc.DescriptionType { Value = l.Product.Description }],
            SellersItemIdentification = new Cac.ItemIdentificationType { Id = new Cbc.IdType { Value = l.Product.Code } },
        };
        if (l.Product.UnspscCode is { } unspsc)
            item.CommodityClassification =
            [
                new Cac.CommodityClassificationType
                {
                    ItemClassificationCode = new Cbc.ItemClassificationCodeType
                    {
                        Value = unspsc, ListId = "UNSPSC", ListAgencyName = "GS1 US", ListName = "Item Classification",
                    },
                },
            ];

        return new Cac.InvoiceLineType
        {
            Id = new Cbc.IdType { Value = l.Order.ToString() },                                        // row 23
            InvoicedQuantity = new Cbc.InvoicedQuantityType                                            // row 24
            {
                Value = l.Quantity, UnitCode = l.Unit, UnitCodeListId = "UN/ECE rec 20", UnitCodeListAgencyName = Unece,
            },
            LineExtensionAmount = Amount<Cbc.LineExtensionAmountType>(l.SaleValue, cur),               // row 25
            PricingReference = new Cac.PricingReferenceType { AlternativeConditionPrice = [price] },
            TaxTotal = [new Cac.TaxTotalType { TaxAmount = Amount<Cbc.TaxAmountType>(l.LineTaxAmount, cur), TaxSubtotal = subs }],
            Item = item,
            Price = new Cac.PriceType { PriceAmount = Amount<Cbc.PriceAmountType>(l.IsFree ? 0m : l.UnitValue, cur) },  // row 27
        };
    }

    private static Cbc.PriceTypeCodeType PriceType(string code) => new()
    {
        Value = code, ListAgencyName = SunatAgency, ListName = "Tipo de Precio", ListUri = CatalogUri + "16",
    };

    private static T Amount<T>(decimal value, string currency) where T : Abc.Ubl.Cct.AmountType, new() =>
        new() { Value = TaxCalculator.R2(value), CurrencyId = currency };

    /// <summary>Up to 10 decimals, trailing zeros removed, at least 2 decimals (unit prices, percentages).</summary>
    public static decimal Fmt(decimal x)
    {
        var n = decimal.Round(x, 10, MidpointRounding.AwayFromZero) / 1.000000000000000000000000000000000m;
        return n.Scale < 2 ? n + 0.00m : n;
    }
}
