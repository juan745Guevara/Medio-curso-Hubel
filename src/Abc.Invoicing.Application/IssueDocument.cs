using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Application;

/// <param name="Observations">SUNAT rules with codes 4000+ broken by the XML (FR-52): the document is still sent.</param>
public sealed record IssueResult(ElectronicDocument Document, SunatStatus Status, SignedXml SignedXml, CdrResult? Cdr,
    IReadOnlyList<RuleViolation> Observations);

/// <summary>
/// Input port IssueDocument (contracts/ports.md) for an already-numbered document: the 3 steps of the second exam
/// in the order of constitution 4 / ADR 0003 — generate the XML from the XSD classes, validate it against the
/// official XSD and SUNAT's rules (stop if either fails), sign, validate again, send. Numbering and the outbox
/// (ADR 0007) wrap this flow in the repository layer, which is not part of this delivery.
/// </summary>
public sealed class IssueDocument(IUblGenerator generator, IXsdValidator validator, ISunatRulesValidator rules, IXmlSigner signer, ISunatValidator sunat)
{
    public async Task<IssueResult> ExecuteAsync(ElectronicDocument document, CancellationToken ct = default)
    {
        var xml = generator.Generate(document);
        var errors = validator.Validate(xml);
        if (errors.Count > 0)
            throw new DomainException("INVALID_XSD", "UBL XML does not validate against the XSD: " + string.Join(" | ", errors));

        var violations = rules.Validate(xml);
        var rejections = violations.Where(v => !v.IsObservation).ToList();
        if (rejections.Count > 0)
            throw new DomainException("SUNAT_RULES", "UBL XML breaks SUNAT validation rules: " + string.Join(" | ", rejections));

        var signed = signer.Sign(xml);
        errors = validator.Validate(signed.Xml);
        if (errors.Count > 0)
            throw new DomainException("INVALID_SIGNATURE", "Signed XML no longer validates: " + string.Join(" | ", errors));

        var cdr = await sunat.SendBillAsync(document.FileName, signed.Xml, ct);
        return new IssueResult(document, cdr.Status, signed, cdr, violations);
    }
}
