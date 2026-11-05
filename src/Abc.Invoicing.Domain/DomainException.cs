namespace Abc.Invoicing.Domain;

/// <summary>A business rule was violated. <see cref="Code"/> is the internal code of regulatory-matrix.md §G.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
