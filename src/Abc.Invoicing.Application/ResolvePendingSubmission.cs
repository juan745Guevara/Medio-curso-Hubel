using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Application;

/// <param name="Resent">True when the signed file was sent again because SUNAT had no CDR for it.</param>
public sealed record ResolveResult(SunatStatus Status, CdrResult? Cdr, bool Resent);

/// <summary>
/// FR-11 / ADR 0007, for a Document left in PENDING_RESEND after a lost answer: first ask SUNAT for its CDR
/// (getStatusCdr) and, only if SUNAT has none, resend <b>the same signed file with the same number</b> (FR-45).
/// So a document SUNAT did receive is never sent twice, and a number is never consumed by a second submission.
/// The dispatcher of the outbox calls this on each retry.
/// </summary>
public sealed class ResolvePendingSubmission(ISunatValidator sunat)
{
    public async Task<ResolveResult> ExecuteAsync(ElectronicDocument document, SignedXml signed, CancellationToken ct = default)
    {
        CdrResult? cdr;
        try
        {
            cdr = await sunat.GetStatusCdrAsync(document.Issuer.Ruc.Value, document.Type, document.Series, document.Number, ct);
        }
        catch (SunatUnreachableException)
        {
            return new ResolveResult(SunatStatus.PendingResend, null, Resent: false);
        }

        // SUNAT has the CDR (accepted, observed or rejected), or the query failed with an exception code: no resend.
        if (cdr is not null)
            return new ResolveResult(cdr.Status, cdr, Resent: false);

        try
        {
            cdr = await sunat.SendBillAsync(document.FileName, signed.Xml, ct);
            return new ResolveResult(cdr.Status, cdr, Resent: true);
        }
        catch (SunatUnreachableException)
        {
            return new ResolveResult(SunatStatus.PendingResend, null, Resent: true);
        }
    }
}
