namespace Abc.Invoicing.Domain;

public sealed record Address(string Ubigeo, string Department, string Province, string District, string Line);

/// <summary>The Issuer (ABC). Establishment code 0000 = tax address (matrix row 15).</summary>
public sealed record Issuer(Ruc Ruc, string BusinessName, string TradeName, string EstablishmentCode, Address Address);

/// <summary>Buyer with its catalog 06 identity document.</summary>
public sealed record Buyer(string DocumentType, string DocumentNumber, string Name, string? Address = null)
{
    public bool HasRuc => DocumentType == "6";
}

public sealed record Installment(decimal Amount, DateOnly DueDate);

/// <summary>FR-12: Cash, or Credit with outstanding amount and installments (RS 193-2020).</summary>
public sealed record PaymentTerms(bool IsCredit, decimal OutstandingAmount, IReadOnlyList<Installment> Installments)
{
    public static PaymentTerms Cash() => new(false, 0m, []);

    public static PaymentTerms Credit(decimal outstanding, params Installment[] installments) => new(true, outstanding, installments);
}
