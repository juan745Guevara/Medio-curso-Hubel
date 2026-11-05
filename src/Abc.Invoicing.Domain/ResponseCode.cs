namespace Abc.Invoicing.Domain;

public enum SunatStatus { Draft, Validated, Signed, Sent, Accepted, Observed, Rejected, PendingResend, Voided }

/// <summary>FR-45 / ADR 0007: SUNAT return code → status, by range (catalogs/sunat-return-codes.json).</summary>
public static class ResponseCodeClassifier
{
    /// <param name="code">ResponseCode of the CDR, or the code of the SOAP fault.</param>
    /// <param name="observationCount">Number of cbc:Note observations (codes 4000+) in an accepted CDR.</param>
    public static SunatStatus Classify(int code, int observationCount = 0) => code switch
    {
        0 => observationCount > 0 ? SunatStatus.Observed : SunatStatus.Accepted,
        >= 100 and <= 1999 => SunatStatus.PendingResend, // exception: not processed, resend the same signed file
        >= 2000 and <= 3999 => SunatStatus.Rejected,     // rejection: the number is consumed
        >= 4000 => SunatStatus.Observed,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Not a SUNAT return code"),
    };
}
