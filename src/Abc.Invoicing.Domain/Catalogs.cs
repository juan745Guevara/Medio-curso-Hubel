namespace Abc.Invoicing.Domain;

/// <summary>Catalog 01 (only the types in scope).</summary>
public enum DocumentType { Invoice = 1, SalesReceipt = 3, CreditNote = 7, DebitNote = 8 }

/// <summary>Catalog 08 — ISC calculation system.</summary>
public enum IscSystem { AdValorem = 1, FixedAmount = 2, RetailPrice = 3 }

public static class Catalogs
{
    /// <summary>Catalog 01 code, two digits.</summary>
    public static string Code(this DocumentType t) => ((int)t).ToString("00");

    public static string Code(this IscSystem s) => ((int)s).ToString("00");

    /// <summary>Catalog 07 codes of free-of-charge transfers (FR-47).</summary>
    public static readonly IReadOnlySet<string> FreeOfChargeTreatments =
        new HashSet<string> { "11", "12", "13", "14", "15", "16", "21", "31", "32", "33", "34", "35", "36", "37" };

    /// <summary>Free-of-charge treatments that still carry an IGV amount (taxed withdrawals 11–16).</summary>
    public static readonly IReadOnlySet<string> FreeTaxedTreatments = new HashSet<string> { "11", "12", "13", "14", "15", "16" };

    public static readonly IReadOnlySet<string> IgvTreatments = new HashSet<string>
        { "10", "11", "12", "13", "14", "15", "16", "17", "20", "21", "30", "31", "32", "33", "34", "35", "36", "37", "40" };

    /// <summary>Catalog 03 commercial units accepted by ABC (same list as UnitOfMeasure in common.schema.json).</summary>
    public static readonly IReadOnlySet<string> Units = new HashSet<string>
    {
        "4A", "BJ", "BLL", "BG", "BO", "BX", "CT", "CMK", "CMQ", "CMT", "CEN", "CY", "CJ", "DZN", "DZP", "BE", "GLI", "GRM",
        "GRO", "HLT", "LEF", "SET", "KGM", "KTM", "KWH", "KT", "CA", "LBR", "LTR", "MWH", "MTR", "MTK", "MTQ", "MGM", "MLT",
        "MMT", "MMK", "MMQ", "MLL", "UM", "ONZ", "PF", "PK", "PR", "FOT", "FTK", "FTQ", "C62", "PG", "ST", "INH", "RM", "DR",
        "STN", "LTN", "TNE", "TU", "NIU", "ZZ", "GLL", "YRD", "YDK",
    };

    /// <summary>Catalog 05 tax scheme: (code, name, UN/ECE 5153 type).</summary>
    public static (string Code, string Name, string Type) TaxScheme(string code) => code switch
    {
        "1000" => ("1000", "IGV", "VAT"),
        "1016" => ("1016", "IVAP", "VAT"),
        "2000" => ("2000", "ISC", "EXC"),
        "7152" => ("7152", "ICBPER", "OTH"),
        "9995" => ("9995", "EXP", "FRE"),
        "9996" => ("9996", "GRA", "FRE"),
        "9997" => ("9997", "EXO", "VAT"),
        "9998" => ("9998", "INA", "FRE"),
        "9999" => ("9999", "OTROS", "OTH"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Not a catalog 05 code"),
    };
}
