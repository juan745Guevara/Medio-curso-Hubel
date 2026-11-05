using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Application;

public sealed record IssueResult(ElectronicDocument Document, SunatStatus Status, SignedXml SignedXml, CdrResult? Cdr);

/// <summary>
/// Input port IssueDocument (contracts/ports.md) for an already-numbered document: the 3 steps of the second exam
/// in the order of constitution 4 / ADR 0003 — generate the XML from the XSD classes, validate it against the
/// official XSD (stop if it fails), sign, validate again, send. Numbering and the outbox (ADR 0007) wrap this
/// flow in the repository layer, which is not part of this delivery.
/// </summary>
public sealed class IssueDocument(IUblGenerator generator, IXsdValidator validator, IXmlSigner signer, ISunatValidator sunat)
{
    public async Task<IssueResult> ExecuteAsync(ElectronicDocument document, CancellationToken ct = default)
    {
        var xml = generator.Generate(document);
        var errors = validator.Validate(xml);
        if (errors.Count > 0)
            throw new DomainException("INVALID_XSD", "UBL XML does not validate against the XSD: " + string.Join(" | ", errors));

        var signed = signer.Sign(xml);
        errors = validator.Validate(signed.Xml);
        if (errors.Count > 0)
            throw new DomainException("INVALID_SIGNATURE", "Signed XML no longer validates: " + string.Join(" | ", errors));

        var cdr = await sunat.SendBillAsync(document.FileName, signed.Xml, ct);
        return new IssueResult(document, cdr.Status, signed, cdr);
    }
}
