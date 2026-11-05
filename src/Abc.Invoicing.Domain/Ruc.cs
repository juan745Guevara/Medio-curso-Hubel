namespace Abc.Invoicing.Domain;

/// <summary>RUC value object (FR-02): 11 digits, prefix 10/15/16/17/20, modulo-11 check digit.</summary>
public readonly record struct Ruc
{
    private static readonly int[] Weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
    private static readonly string[] Prefixes = ["10", "15", "16", "17", "20"];

    public string Value { get; }

    public Ruc(string value)
    {
        if (!IsValid(value))
            throw new DomainException("INVALID_RUC", $"Invalid RUC: {value}");
        Value = value;
    }

    public static bool IsValid(string? ruc)
    {
        if (ruc is null || ruc.Length != 11 || !ruc.All(char.IsAsciiDigit) || !Prefixes.Contains(ruc[..2]))
            return false;
        var sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (ruc[i] - '0') * Weights[i];
        var dv = 11 - sum % 11;
        dv = dv switch { 10 => 0, 11 => 1, _ => dv };
        return dv == ruc[10] - '0';
    }

    public override string ToString() => Value;
}
