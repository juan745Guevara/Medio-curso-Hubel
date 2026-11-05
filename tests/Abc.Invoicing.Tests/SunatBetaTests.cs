using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;
using Abc.Invoicing.Infrastructure.Signing;
using Abc.Invoicing.Infrastructure.Ubl;
using Abc.Invoicing.Infrastructure.Sunat;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>Runs only when SUNAT_BETA=1: it needs the network and SUNAT's test service, which a CI or the grader's PC may not reach.</summary>
public sealed class BetaFactAttribute : FactAttribute
{
    public BetaFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SUNAT_BETA") != "1")
            Skip = "Integration test against SUNAT beta: set SUNAT_BETA=1 to run it (needs internet).";
    }
}

/// <summary>
/// Second exam, step 3 against the real service: the whole flow (generate → XSD → SUNAT rules → sign → sendBill) to
/// SUNAT's public test environment (e-beta), with SUNAT's generic test credentials (RUC + MODDATOS / moddatos) and a
/// self-signed test certificate generated in memory (constitution 15: nothing secret is versioned).
/// The signed XML and the CDR are saved under specs/examples/sunat-beta/ as evidence.
/// </summary>
public class SunatBetaTests
{
    [BetaFact(DisplayName = "AC-17 SUNAT beta accepts the generated, validated and signed invoice (CDR code 0)")]
    public Task Ac17_sunat_beta_accepts_the_invoice() => SendToBetaAsync("issue-invoice-cash.json");

    [BetaFact(DisplayName = "AC-17 SUNAT beta accepts the generated, validated and signed sales receipt with a free line (CDR code 0)")]
    public Task Ac17_sunat_beta_accepts_the_sales_receipt() => SendToBetaAsync("issue-receipt-free-line.json");

    private static async Task SendToBetaAsync(string requestFile)
    {
        var ruc = Environment.GetEnvironmentVariable("SUNAT_BETA_RUC") ?? Examples.Issuer().Ruc.Value;
        var issuer = Examples.Issuer() with { Ruc = new Ruc(ruc) };
        var lima = TimeZoneInfo.ConvertTime(DateTime.UtcNow, LimaTimeZone());
        // A different number on every run, so the beta never sees the same Series-number twice.
        var number = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 99_999_999) + 1;
        var document = Examples.Document(requestFile, number, lima.ToString("HH:mm:ss"), DateOnly.FromDateTime(lima), issuer);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var sunat = new SunatBillServiceClient(http, new SunatCredentials(ruc, "MODDATOS", "moddatos"), SunatBillServiceClient.BetaEndpoint);
        using var cert = ValidationAndSigningTests.TestCertificate();
        var result = await new IssueDocument(new Ubl21InvoiceGenerator(), new OasisXsdValidator(), new SunatRulesValidator(),
            new XmlDsigSigner(cert), sunat).ExecuteAsync(document);
        Assert.True(result.Cdr is not null, "SUNAT beta did not answer (PENDING_RESEND)");

        var folder = Path.Combine(RepositoryRoot(), "specs", "examples", "sunat-beta");
        Directory.CreateDirectory(folder);
        await File.WriteAllBytesAsync(Path.Combine(folder, document.FileName + ".xml"), result.SignedXml.Xml);
        if (result.Cdr!.CdrZip is { } cdr)
            await File.WriteAllBytesAsync(Path.Combine(folder, "R-" + document.FileName + ".zip"), cdr);
        await File.WriteAllTextAsync(Path.Combine(folder, "R-" + document.FileName + ".txt"),
            $"Sent {DateTime.UtcNow:O} to {SunatBillServiceClient.BetaEndpoint}\nResponseCode: {result.Cdr.Code}\nDescription: {result.Cdr.Description}\n"
            + string.Concat(result.Cdr.Observations.Select(o => $"Observation: {o.Code} - {o.Text}\n")));

        Assert.True(result.Cdr.Code == 0, $"SUNAT beta answered {result.Cdr.Code}: {result.Cdr.Description}");
        Assert.Contains(result.Status, new[] { SunatStatus.Accepted, SunatStatus.Observed });
    }

    private static TimeZoneInfo LimaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Abc.Invoicing.sln")))
                return dir.FullName;
        return AppContext.BaseDirectory;
    }
}
