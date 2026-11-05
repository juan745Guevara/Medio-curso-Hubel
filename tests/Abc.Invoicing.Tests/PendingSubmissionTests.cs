using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;
using Abc.Invoicing.Infrastructure.Signing;
using Abc.Invoicing.Infrastructure.Sunat;
using Abc.Invoicing.Infrastructure.Ubl;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>
/// FR-11 / ADR 0007: a lost answer leaves the Document in PENDING_RESEND, and before resending the system asks SUNAT
/// for its CDR (getStatusCdr), so a document SUNAT already received is never sent twice.
/// </summary>
public class PendingSubmissionTests
{
    private static readonly SunatCredentials Credentials = new("20131312955", "MODDATOS", "moddatos");

    /// <summary>Fake SUNAT that answers sendBill and getStatusCdr by SOAPAction and records every call.</summary>
    private sealed class FakeSunat(Func<string, string, string> answer) : HttpMessageHandler
    {
        public List<(Uri Uri, string Action, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var action = request.Headers.GetValues("SOAPAction").Single();
            var body = await request.Content!.ReadAsStringAsync(ct);
            Calls.Add((request.RequestUri!, action, body));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer(action, body), Encoding.UTF8, "text/xml") };
        }
    }

    /// <summary>A handler that never answers, as when SUNAT hangs: HttpClient.Timeout ends the call.</summary>
    private sealed class Silent : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        }
    }

    private static string StatusCdr(string statusCode, string message, string? cdrZipBase64) =>
        "<S:Envelope xmlns:S=\"http://schemas.xmlsoap.org/soap/envelope/\"><S:Body><ns2:getStatusCdrResponse xmlns:ns2=\"http://service.sunat.gob.pe\"><statusCdr>"
        + (cdrZipBase64 is null ? "" : $"<content>{cdrZipBase64}</content>")
        + $"<statusCode>{statusCode}</statusCode><statusMessage>{message}</statusMessage></statusCdr></ns2:getStatusCdrResponse></S:Body></S:Envelope>";

    private static string CdrZipBase64(string answer) =>
        XDocument.Parse(answer).Descendants("applicationResponse").Single().Value;

    private static SunatBillServiceClient Client(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(30) }, Credentials,
            SunatBillServiceClient.BetaEndpoint, SunatBillServiceClient.ConsultProductionEndpoint);

    private static SignedXml Signed(ElectronicDocument doc) =>
        new XmlDsigSigner(ValidationAndSigningTests.TestCertificate()).Sign(new Ubl21InvoiceGenerator().Generate(doc));

    [Fact(DisplayName = "AC-19 SUNAT does not answer in time → PENDING_RESEND, signed file kept, no CDR")]
    public async Task Ac19_no_answer_leaves_pending_resend()
    {
        var client = Client(new Silent(), TimeSpan.FromMilliseconds(200));
        var useCase = new IssueDocument(new Ubl21InvoiceGenerator(), new OasisXsdValidator(), new SunatRulesValidator(),
            new XmlDsigSigner(ValidationAndSigningTests.TestCertificate()), client);
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.Equal(SunatStatus.PendingResend, result.Status);
        Assert.Null(result.Cdr);
        Assert.True(XmlDsigSigner.Verify(result.SignedXml.Xml));
    }

    [Fact(DisplayName = "getStatusCdr envelope: billConsultService, SOAPAction urn:getStatusCdr, RUC, type, Series, number")]
    public async Task GetStatusCdr_envelope()
    {
        var fake = new FakeSunat((_, _) => StatusCdr("0011", "El comprobante no existe", null));
        await Client(fake).GetStatusCdrAsync("20131312955", DocumentType.Invoice, "F001", 42);
        var call = Assert.Single(fake.Calls);
        Assert.Equal(SunatBillServiceClient.ConsultProductionEndpoint, call.Uri);
        Assert.Equal("urn:getStatusCdr", call.Action);
        var body = XDocument.Parse(call.Body);
        XNamespace ser = "http://service.sunat.gob.pe";
        var op = body.Descendants(ser + "getStatusCdr").Single();
        Assert.Equal(["20131312955", "01", "F001", "42"],
            new[] { "rucComprobante", "tipoComprobante", "serieComprobante", "numeroComprobante" }.Select(n => op.Element(n)!.Value));
        Assert.Contains("20131312955MODDATOS", call.Body);
    }

    [Fact(DisplayName = "AC-79 SUNAT already has the CDR → its status is recorded and the document is NOT sent again")]
    public async Task Ac79_cdr_found_is_not_resent()
    {
        var cdr = CdrZipBase64(SendingTests.CdrAnswer("20131312955-01-F001-42", 0, "La Factura numero F001-42, ha sido aceptada"));
        var fake = new FakeSunat((action, _) => action == "urn:getStatusCdr"
            ? StatusCdr("0004", "La constancia existe", cdr)
            : throw new InvalidOperationException("sendBill must not be called"));
        var doc = Examples.Cash42();
        var result = await new ResolvePendingSubmission(Client(fake)).ExecuteAsync(doc, Signed(doc));
        Assert.Equal(SunatStatus.Accepted, result.Status);
        Assert.False(result.Resent);
        Assert.Contains("aceptada", result.Cdr!.Description);
        Assert.Equal(["urn:getStatusCdr"], fake.Calls.Select(c => c.Action));
    }

    [Fact(DisplayName = "AC-79 A rejection CDR found by getStatusCdr is recorded as REJECTED, without resending")]
    public async Task Ac79_rejected_cdr_found()
    {
        var cdr = CdrZipBase64(SendingTests.CdrAnswer("20131312955-01-F001-42", 2800, "El dato ingresado en el tipo de documento de identidad del receptor no esta permitido."));
        var fake = new FakeSunat((action, _) => action == "urn:getStatusCdr" ? StatusCdr("0004", "La constancia existe", cdr) : throw new InvalidOperationException());
        var doc = Examples.Cash42();
        var result = await new ResolvePendingSubmission(Client(fake)).ExecuteAsync(doc, Signed(doc));
        Assert.Equal(SunatStatus.Rejected, result.Status);
        Assert.False(result.Resent);
    }

    [Fact(DisplayName = "AC-80 SUNAT has no CDR → the SAME signed file is sent again with the same number")]
    public async Task Ac80_no_cdr_resends_the_same_signed_file()
    {
        var fake = new FakeSunat((action, _) => action == "urn:getStatusCdr"
            ? StatusCdr("0011", "El comprobante de pago electrónico no existe", null)
            : SendingTests.CdrAnswer("20131312955-01-F001-42", 0, "La Factura numero F001-42, ha sido aceptada"));
        var doc = Examples.Cash42();
        var signed = Signed(doc);
        var result = await new ResolvePendingSubmission(Client(fake)).ExecuteAsync(doc, signed);
        Assert.Equal(SunatStatus.Accepted, result.Status);
        Assert.True(result.Resent);
        Assert.Equal(["urn:getStatusCdr", "urn:sendBill"], fake.Calls.Select(c => c.Action));

        // The resent ZIP holds exactly the stored signed bytes, under the same file name.
        XNamespace ser = "http://service.sunat.gob.pe";
        var send = XDocument.Parse(fake.Calls[1].Body).Descendants(ser + "sendBill").Single();
        Assert.Equal("20131312955-01-F001-42.zip", send.Element("fileName")!.Value);
        using var zip = new ZipArchive(new MemoryStream(Convert.FromBase64String(send.Element("contentFile")!.Value)));
        using var ms = new MemoryStream();
        zip.GetEntry("20131312955-01-F001-42.xml")!.Open().CopyTo(ms);
        Assert.Equal(signed.Xml, ms.ToArray());
    }

    [Fact(DisplayName = "The query fails with exception 0109 → stays PENDING_RESEND and is not resent yet")]
    public async Task Query_exception_keeps_pending()
    {
        var fake = new FakeSunat((action, _) => action == "urn:getStatusCdr"
            ? "<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap-env:Body><soap-env:Fault><faultcode>soap-env:Client.0109</faultcode><faultstring>El sistema no puede responder su solicitud.</faultstring></soap-env:Fault></soap-env:Body></soap-env:Envelope>"
            : throw new InvalidOperationException("sendBill must not be called"));
        var doc = Examples.Cash42();
        var result = await new ResolvePendingSubmission(Client(fake)).ExecuteAsync(doc, Signed(doc));
        Assert.Equal(SunatStatus.PendingResend, result.Status);
        Assert.False(result.Resent);
    }

    [Fact(DisplayName = "The query gets no answer → stays PENDING_RESEND and is not resent yet")]
    public async Task Query_without_answer_keeps_pending()
    {
        var doc = Examples.Cash42();
        var result = await new ResolvePendingSubmission(Client(new Silent(), TimeSpan.FromMilliseconds(200))).ExecuteAsync(doc, Signed(doc));
        Assert.Equal(SunatStatus.PendingResend, result.Status);
        Assert.False(result.Resent);
        Assert.Null(result.Cdr);
    }
}
