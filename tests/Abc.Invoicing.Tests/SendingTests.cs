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
/// Second exam, step 3: sending. SUNAT is replaced by a fake HTTP handler (no network, no SOL credentials):
/// the tests check the ZIP, the SOAP sendBill envelope and how each kind of answer becomes a status (FR-45).
/// </summary>
public class SendingTests
{
    private static readonly SunatCredentials Credentials = new("20131312955", "MODDATOS", "moddatos");

    private sealed class FakeSunat(Func<HttpRequestMessage, string> answer) : HttpMessageHandler
    {
        public string? LastBody { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer(request), Encoding.UTF8, "text/xml") };
        }
    }

    internal static string CdrAnswer(string fileName, int code, string description, params string[] notes)
    {
        XNamespace ar = "urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        var cdr = new XDocument(new XElement(ar + "ApplicationResponse",
            new XElement(cbc + "ID", "1"),
            notes.Select(n => new XElement(cbc + "Note", n)),
            new XElement(cac + "DocumentResponse", new XElement(cac + "Response",
                new XElement(cbc + "ReferenceID", fileName), new XElement(cbc + "ResponseCode", code), new XElement(cbc + "Description", description)))));
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        using (var s = zip.CreateEntry("R-" + fileName + ".xml").Open())
            cdr.Save(s);
        return $"<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap-env:Body><br:sendBillResponse xmlns:br=\"http://service.sunat.gob.pe\"><applicationResponse>{Convert.ToBase64String(ms.ToArray())}</applicationResponse></br:sendBillResponse></soap-env:Body></soap-env:Envelope>";
    }

    private static string Fault(string code, string text) =>
        $"<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap-env:Body><soap-env:Fault><faultcode>soap-env:Client.{code}</faultcode><faultstring>{text}</faultstring></soap-env:Fault></soap-env:Body></soap-env:Envelope>";

    private static (IssueDocument UseCase, FakeSunat Fake) Build(Func<HttpRequestMessage, string> answer)
    {
        var fake = new FakeSunat(answer);
        var client = new SunatBillServiceClient(new HttpClient(fake), Credentials, SunatBillServiceClient.BetaEndpoint);
        var cert = ValidationAndSigningTests.TestCertificate();
        return (new IssueDocument(new Ubl21InvoiceGenerator(), new OasisXsdValidator(), new SunatRulesValidator(), new XmlDsigSigner(cert), client), fake);
    }

    [Fact(DisplayName = "ZIP named RUC-TT-SERIES-NUMBER.zip holds the signed XML RUC-TT-SERIES-NUMBER.xml")]
    public void Zip_contains_the_xml()
    {
        var zip = SunatBillServiceClient.Zip("20131312955-01-F001-42", "<Invoice/>"u8.ToArray());
        using var archive = new ZipArchive(new MemoryStream(zip));
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("20131312955-01-F001-42.xml", entry.FullName);
    }

    [Fact(DisplayName = "sendBill envelope: WS-Security user RUC+SOL user, fileName .zip, base64 content")]
    public async Task SendBill_envelope()
    {
        var (useCase, fake) = Build(r => CdrAnswer("20131312955-01-F001-42", 0, "La Factura numero F001-42, ha sido aceptada"));
        await useCase.ExecuteAsync(Examples.Cash42());
        var env = XDocument.Parse(fake.LastBody!);
        XNamespace wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        XNamespace ser = "http://service.sunat.gob.pe";
        Assert.Equal("20131312955MODDATOS", env.Descendants(wsse + "Username").Single().Value);
        Assert.Equal("20131312955-01-F001-42.zip", env.Descendants(ser + "sendBill").Elements("fileName").Single().Value);
        var content = Convert.FromBase64String(env.Descendants(ser + "sendBill").Elements("contentFile").Single().Value);
        using var archive = new ZipArchive(new MemoryStream(content));
        using var s = archive.Entries.Single().Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        Assert.True(XmlDsigSigner.Verify(ms.ToArray()));
        Assert.Equal(SunatBillServiceClient.BetaEndpoint, fake.LastRequest!.RequestUri);
        Assert.Equal("urn:sendBill", fake.LastRequest.Headers.GetValues("SOAPAction").Single());
    }

    [Fact(DisplayName = "AC-17 CDR code 0 → ACCEPTED")]
    public async Task Ac17_accepted()
    {
        var (useCase, _) = Build(_ => CdrAnswer("20131312955-01-F001-42", 0, "La Factura numero F001-42, ha sido aceptada"));
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.Equal(SunatStatus.Accepted, result.Status);
        Assert.Contains("aceptada", result.Cdr!.Description);
    }

    [Fact(DisplayName = "AC-63 CDR code 0 with observation 4287 → OBSERVED")]
    public async Task Ac63_observed()
    {
        var (useCase, _) = Build(_ => CdrAnswer("20131312955-01-F001-42", 0, "Aceptada con observaciones",
            "4287 - El precio unitario de la operación que está informando difiere de los cálculos realizados"));
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.Equal(SunatStatus.Observed, result.Status);
        Assert.Equal("4287", Assert.Single(result.Cdr!.Observations).Code);
    }

    [Fact(DisplayName = "AC-62 CDR code 2800 → REJECTED")]
    public async Task Ac62_rejected()
    {
        var (useCase, _) = Build(_ => CdrAnswer("20131312955-01-F001-42", 2800, "El dato ingresado en el tipo de documento de identidad del receptor no esta permitido."));
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.Equal(SunatStatus.Rejected, result.Status);
        Assert.Equal(2800, result.Cdr!.Code);
    }

    [Fact(DisplayName = "AC-61 SOAP fault 0109 (service down) → PENDING_RESEND, same signed file kept")]
    public async Task Ac61_exception_keeps_pending()
    {
        var (useCase, _) = Build(_ => Fault("0109", "El sistema no puede responder su solicitud. (El servicio de autenticación no está disponible)"));
        var result = await useCase.ExecuteAsync(Examples.Cash42());
        Assert.Equal(SunatStatus.PendingResend, result.Status);
        Assert.True(XmlDsigSigner.Verify(result.SignedXml.Xml));
    }

    [Fact(DisplayName = "Nothing is sent when the XML does not validate (constitution 4)")]
    public async Task Nothing_is_sent_when_invalid()
    {
        var fake = new FakeSunat(_ => throw new InvalidOperationException("must not be called"));
        var client = new SunatBillServiceClient(new HttpClient(fake), Credentials, SunatBillServiceClient.BetaEndpoint);
        var broken = new BrokenGenerator();
        var useCase = new IssueDocument(broken, new OasisXsdValidator(), new SunatRulesValidator(), new XmlDsigSigner(ValidationAndSigningTests.TestCertificate()), client);
        var ex = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(Examples.Cash42()));
        Assert.Equal("INVALID_XSD", ex.Code);
        Assert.Null(fake.LastRequest);
    }

    private sealed class BrokenGenerator : IUblGenerator
    {
        public byte[] Generate(ElectronicDocument document)
        {
            var x = new Xml(new Ubl21InvoiceGenerator().Generate(document));
            x.Remove("/inv:Invoice/cbc:IssueDate");
            return x.Bytes();
        }
    }
}
