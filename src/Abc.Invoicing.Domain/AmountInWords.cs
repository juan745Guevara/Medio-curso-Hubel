namespace Abc.Invoicing.Domain;

/// <summary>Legend 1000 (catalog 52), in Spanish as SUNAT requires (FR-15): "SETENTA Y NUEVE CON 00/100 SOLES".</summary>
public static class AmountInWords
{
    private static readonly string[] Units =
    [
        "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE",
        "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIUNO", "VEINTIDOS",
        "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE",
    ];
    private static readonly string[] Tens = ["", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"];
    private static readonly string[] Hundreds =
        ["", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"];

    private static string Below1000(int n)
    {
        if (n == 0) return "";
        if (n == 100) return "CIEN";
        var c = n / 100;
        var rest = n % 100;
        string tail;
        if (rest < 30) tail = Units[rest];
        else tail = Tens[rest / 10] + (rest % 10 != 0 ? " Y " + Units[rest % 10] : "");
        return string.Join(" ", new[] { Hundreds[c], tail }.Where(s => s.Length > 0));
    }

    public static string Number(long n)
    {
        if (n == 0) return "CERO";
        var millions = (int)(n / 1_000_000);
        var thousands = (int)(n / 1000 % 1000);
        var units = (int)(n % 1000);
        var parts = new List<string>();
        if (millions > 0) parts.Add(millions == 1 ? "UN MILLON" : $"{Below1000(millions)} MILLONES");
        if (thousands > 0) parts.Add(thousands == 1 ? "MIL" : $"{Below1000(thousands)} MIL");
        if (units > 0) parts.Add(Below1000(units));
        return string.Join(" ", parts);
    }

    public static string Of(decimal amount, string currency)
    {
        amount = TaxCalculator.R2(amount);
        var whole = (long)decimal.Truncate(amount);
        var cents = (int)((amount - whole) * 100);
        var name = currency switch
        {
            "PEN" => "SOLES",
            "USD" => "DOLARES AMERICANOS",
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Only PEN and USD in scope"),
        };
        return $"{Number(whole)} CON {cents:00}/100 {name}";
    }
}
