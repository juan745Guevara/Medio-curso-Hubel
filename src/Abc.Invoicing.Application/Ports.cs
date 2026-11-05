using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Application;

/// <summary>UblGeneratorPort (contracts/ports.md): domain document → UBL 2.1 XML, unsigned, with the signature slot.</summary>
public interface IUblGenerator
{
    byte[] Generate(ElectronicDocument document);
}

/// <summary>XsdValidatorPort (FR-08, ADR 0003): returns the structure errors; empty = valid.</summary>
public interface IXsdValidator
{
    IReadOnlyList<string> Validate(byte[] xml);
}

/// <summary>A SUNAT validation rule broken by the XML, with SUNAT's own return code (sunat-return-codes.json).</summary>
public sealed record RuleViolation(string Code, string Message)
{
    /// <summary>Codes 4000 and above are observations: SUNAT accepts the document and notes them (FR-45).</summary>
    public bool IsObservation => int.Parse(Code, System.Globalization.CultureInfo.InvariantCulture) >= 4000;

    public override string ToString() => $"{Code} - {Message}";
}

/// <summary>
/// SunatRulesValidatorPort (FR-52): SUNAT's business rules that the XSD cannot express (regulatory matrix, codes
/// 2000–3999 reject, 4000+ observe), checked on the XML before signing. Empty = no rule broken.
/// </summary>
public interface ISunatRulesValidator
{
    IReadOnlyList<RuleViolation> Validate(byte[] xml);
}

public sealed record SignedXml(byte[] Xml, string DigestValue);

/// <summary>SignerPort (FR-09, ADR 0006): XMLDSig enveloped inside ext:UBLExtensions.</summary>
public interface IXmlSigner
{
    SignedXml Sign(byte[] xml);
}

public sealed record CdrResult(int Code, string Description, IReadOnlyList<(string Code, string Text)> Observations, byte[]? CdrZip)
{
    public SunatStatus Status => ResponseCodeClassifier.Classify(Code, Observations.Count);
}

/// <summary>The SUNAT validator could not be reached or did not answer in time (FR-11): the outcome is unknown.</summary>
public sealed class SunatUnreachableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>SunatValidatorPort (FR-10, FR-11): sendBill to SUNAT or an OSE; one adapter per provider (NFR-07).</summary>
public interface ISunatValidator
{
    /// <exception cref="SunatUnreachableException">No answer: the document may or may not have been received.</exception>
    Task<CdrResult> SendBillAsync(string fileName, byte[] signedXml, CancellationToken ct = default);

    /// <summary>
    /// getStatusCdr (ADR 0007): the CDR SUNAT already has for this document, or <c>null</c> when it has none (the
    /// document never arrived, so it can be resent). A result with an exception code means the query itself failed.
    /// </summary>
    /// <exception cref="SunatUnreachableException">No answer to the query.</exception>
    Task<CdrResult?> GetStatusCdrAsync(string ruc, DocumentType type, string series, int number, CancellationToken ct = default);
}
