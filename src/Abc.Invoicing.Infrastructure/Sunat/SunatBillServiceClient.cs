using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Abc.Invoicing.Application;
using Abc.Invoicing.Domain;

namespace Abc.Invoicing.Infrastructure.Sunat;

public sealed record SunatCredentials(string Ruc, string SolUser, string SolPassword)
{
    /// <summary>WS-Security user: RUC followed by the SOL user (Annex 244-2019).</summary>
    public string Username => Ruc + SolUser;
}

/// <summary>
/// Adapter of <see cref="ISunatValidator"/> for SUNAT's billService (SEE-Del contribuyente, contracts/ports.md):
/// ZIP named RUC-TT-SERIES-NUMBER.zip with the signed XML inside, SOAP sendBill with WS-Security UsernameToken,
/// CDR read from the applicationResponse ZIP; SOAP faults are SUNAT exception codes (0100–1999, FR-45).
/// getStatusCdr goes to billConsultService (ADR 0007), which SUNAT offers in production only (the beta has none).
/// The endpoints are configuration (NFR-07); an OSE is another adapter of the same port.
/// </summary>
public sealed class SunatBillServiceClient(HttpClient http, SunatCredentials credentials, Uri endpoint, Uri? consultEndpoint = null) : ISunatValidator
{
    public static readonly Uri BetaEndpoint = new("https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService");
    public static readonly Uri ProductionEndpoint = new("https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService");

    /// <summary>billConsultService (getStatus / getStatusCdr); WSDL as kept by Greenter, packages/ws/src/Resources/wsdl.</summary>
    public static readonly Uri ConsultProductionEndpoint = new("https://e-factura.sunat.gob.pe/ol-it-wsconscpegem/billConsultService");

    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Ser = "http://service.sunat.gob.pe";
    private static readonly XNamespace Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    public static byte[] Zip(string fileName, byte[] signedXml)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(fileName + ".xml", CompressionLevel.Optimal);
            using var s = entry.Open();
            s.Write(signedXml);
        }
        return ms.ToArray();
    }

    public string BuildSendBillEnvelope(string fileName, byte[] zip) =>
        Envelope(new XElement(Ser + "sendBill",
            new XElement("fileName", fileName + ".zip"),
            new XElement("contentFile", Convert.ToBase64String(zip))));

    /// <summary>getStatusCdr request: RUC, document type (catalog 01), Series and number of the document.</summary>
    public string BuildGetStatusCdrEnvelope(string ruc, DocumentType type, string series, int number) =>
        Envelope(new XElement(Ser + "getStatusCdr",
            new XElement("rucComprobante", ruc),
            new XElement("tipoComprobante", type.Code()),
            new XElement("serieComprobante", series),
            new XElement("numeroComprobante", number)));

    private string Envelope(XElement operation) =>
        new XDocument(
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap), new XAttribute(XNamespace.Xmlns + "ser", Ser),
                new XAttribute(XNamespace.Xmlns + "wsse", Wsse),
                new XElement(Soap + "Header",
                    new XElement(Wsse + "Security",
                        new XElement(Wsse + "UsernameToken",
                            new XElement(Wsse + "Username", credentials.Username),
                            new XElement(Wsse + "Password", credentials.SolPassword)))),
                new XElement(Soap + "Body", operation)))
            .ToString(SaveOptions.DisableFormatting);

    private async Task<string> PostAsync(Uri target, string soapAction, string envelope, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, target) { Content = new StringContent(envelope, Encoding.UTF8) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
        request.Headers.Add("SOAPAction", soapAction);
        try
        {
            using var response = await http.SendAsync(request, ct);
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException e)
        {
            throw new SunatUnreachableException($"SUNAT not reachable at {target}: {e.Message}", e);
        }
        catch (TaskCanceledException e) when (!ct.IsCancellationRequested)
        {
            // HttpClient.Timeout elapsed (FR-11: no answer in 30 s).
            throw new SunatUnreachableException($"SUNAT did not answer in {http.Timeout.TotalSeconds:0} s at {target}", e);
        }
    }

    public async Task<CdrResult> SendBillAsync(string fileName, byte[] signedXml, CancellationToken ct = default)
    {
        var body = await PostAsync(endpoint, "urn:sendBill", BuildSendBillEnvelope(fileName, Zip(fileName, signedXml)), ct);
        return ParseResponse(body);
    }

    public async Task<CdrResult?> GetStatusCdrAsync(string ruc, DocumentType type, string series, int number, CancellationToken ct = default)
    {
        var target = consultEndpoint ?? throw new InvalidOperationException(
            "No billConsultService configured: SUNAT offers getStatusCdr in production only (ConsultProductionEndpoint).");
        var body = await PostAsync(target, "urn:getStatusCdr", BuildGetStatusCdrEnvelope(ruc, type, series, number), ct);
        return ParseStatusCdrResponse(body);
    }

    /// <summary>
    /// getStatusCdrResponse/statusCdr: with <c>content</c> it carries the CDR ZIP, whose ResponseCode decides the status;
    /// without it SUNAT has no CDR for the document (null). A SOAP fault is an exception code (the query failed).
    /// </summary>
    public static CdrResult? ParseStatusCdrResponse(string soapBody)
    {
        var x = XDocument.Parse(soapBody);
        if (x.Descendants(Soap + "Fault").Any())
            return ParseResponse(soapBody);
        var status = x.Descendants("statusCdr").FirstOrDefault()
                     ?? throw new XmlException("getStatusCdrResponse without statusCdr");
        var content = (string?)status.Element("content");
        return string.IsNullOrWhiteSpace(content) ? null : ParseCdrZip(Convert.FromBase64String(content));
    }

    /// <summary>Reads either a sendBillResponse (CDR ZIP) or a SOAP fault (exception code, FR-45).</summary>
    public static CdrResult ParseResponse(string soapBody)
    {
        var x = XDocument.Parse(soapBody);
        var fault = x.Descendants(Soap + "Fault").FirstOrDefault();
        if (fault is not null)
        {
            var codeText = (string?)fault.Element("faultcode") ?? "";
            var faultString = (string?)fault.Element("faultstring") ?? "";
            // SUNAT puts the code in faultcode (soap-env:Client.0111) or, with soap-env:Server, in faultstring ("2800").
            var digits = new string(codeText.Where(char.IsAsciiDigit).ToArray());
            if (digits.Length == 0)
                digits = new string(faultString.TakeWhile(char.IsAsciiDigit).ToArray());
            var code = int.TryParse(digits, out var c) ? c : 100;
            return new CdrResult(code, faultString, [], null);
        }
        var b64 = x.Descendants("applicationResponse").FirstOrDefault()?.Value
                  ?? throw new XmlException("sendBillResponse without applicationResponse");
        return ParseCdrZip(Convert.FromBase64String(b64));
    }

    /// <summary>Reads the ApplicationResponse inside a CDR ZIP: ResponseCode, Description and observations (4000+).</summary>
    public static CdrResult ParseCdrZip(byte[] cdrZip)
    {
        using var zip = new ZipArchive(new MemoryStream(cdrZip));
        var xmlEntry = zip.Entries.First(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        using var s = xmlEntry.Open();
        var cdr = XDocument.Load(s);
        var resp = cdr.Descendants(Cac + "DocumentResponse").Elements(Cac + "Response").First();
        var responseCode = int.Parse((string)resp.Element(Cbc + "ResponseCode")!);
        var description = (string?)resp.Element(Cbc + "Description") ?? "";
        var observations = cdr.Root!.Elements(Cbc + "Note")
            .Select(n => n.Value.Split(" - ", 2))
            .Where(p => p.Length == 2 && int.TryParse(p[0], out var oc) && oc >= 4000)
            .Select(p => (p[0], p[1]))
            .ToList();
        return new CdrResult(responseCode, description, observations, cdrZip);
    }
}
