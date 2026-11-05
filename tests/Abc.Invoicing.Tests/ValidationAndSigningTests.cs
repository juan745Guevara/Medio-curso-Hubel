using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Abc.Invoicing.Infrastructure.Signing;
using Abc.Invoicing.Infrastructure.Ubl;
using Xunit;

namespace Abc.Invoicing.Tests;

/// <summary>Second exam, step 2: structural validation against the official schema (ADR 0003), and the signature.</summary>
public class ValidationAndSigningTests
{
    private readonly Ubl21InvoiceGenerator _generator = new();
    private readonly OasisXsdValidator _xsd = new();

    internal static X509Certificate2 TestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=ABC TEST CERTIFICATE - NOT FOR PRODUCTION, O=ABC SOFTWARE CORP S.A.C.", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.Exportable);
    }

    [Fact(DisplayName = "AC-15 An XML without the mandatory IssueDate is rejected by the XSD validator")]
    public void Ac15_missing_issue_date_is_rejected()
    {
        var x = new Xml(_generator.Generate(Examples.Cash42()));
        x.Remove("/inv:Invoice/cbc:IssueDate");
        var errors = _xsd.Validate(x.Bytes());
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("IssueTime", StringComparison.Ordinal) || e.Contains("IssueDate", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "An XML without any InvoiceLine (1..n in the XSD) is rejected")]
    public void Missing_invoice_line_is_rejected()
    {
        var x = new Xml(_generator.Generate(Examples.Cash42()));
        foreach (var line in x.Nodes("/inv:Invoice/cac:InvoiceLine"))
            line.ParentNode!.RemoveChild(line);
        Assert.NotEmpty(_xsd.Validate(x.Bytes()));
    }

    [Fact(DisplayName = "Elements out of XSD order are rejected (the generated classes always write them in order)")]
    public void Elements_out_of_order_are_rejected()
    {
        var x = new Xml(_generator.Generate(Examples.Cash42()));
        var id = x.Node("/inv:Invoice/cbc:ID")!;
        var parent = id.ParentNode!;
        parent.RemoveChild(id);
        parent.AppendChild(id);
        Assert.NotEmpty(_xsd.Validate(x.Bytes()));
    }

    [Fact(DisplayName = "A malformed XML is reported, not thrown")]
    public void Malformed_xml_is_reported()
    {
        Assert.Contains(_xsd.Validate("<Invoice><cbc:ID>"u8.ToArray()), e => e.StartsWith("Not well-formed", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "AC-16 Signature: ds:Signature inside ext:ExtensionContent, verifies, XML still valid against the XSD")]
    public void Ac16_signature()
    {
        using var cert = TestCertificate();
        var signed = new XmlDsigSigner(cert).Sign(_generator.Generate(Examples.Cash42()));
        var x = new Xml(signed.Xml);
        Assert.NotNull(x.Node("/inv:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/ds:Signature"));
        Assert.Equal("SignABC", x.Attr("//ds:Signature", "Id"));
        Assert.Null(x.Node("//*[local-name()='SignaturePlaceholder']"));
        Assert.Empty(_xsd.Validate(signed.Xml));
        Assert.True(XmlDsigSigner.Verify(signed.Xml));
        Assert.False(string.IsNullOrWhiteSpace(signed.DigestValue));
    }

    [Fact(DisplayName = "SignatureValue is valid over SignedInfo canonicalized in place, as SUNAT's verifier (Santuario/xmlsec) does")]
    public void Signature_value_matches_in_place_canonicalization()
    {
        using var cert = TestCertificate();
        var signed = new XmlDsigSigner(cert).Sign(_generator.Generate(Examples.Cash42()));
        var x = new Xml(signed.Xml);
        Assert.Equal("ds", x.Node("//ds:Signature")!.Prefix);
        Assert.DoesNotContain("xmlns=\"http://www.w3.org/2000/09/xmldsig#\"", System.Text.Encoding.UTF8.GetString(signed.Xml));

        // Independent of the signer: the XPath node-set of SignedInfo with its in-scope namespace nodes (XMLDSig §4.3.1).
        var signedInfo = x.Node("//ds:SignedInfo")!;
        var nodes = signedInfo.SelectNodes("(descendant-or-self::node() | descendant-or-self::*/@* | descendant-or-self::*/namespace::*)")!;
        var c14n = new System.Security.Cryptography.Xml.XmlDsigC14NTransform();
        c14n.LoadInput(nodes);
        using var stream = (Stream)c14n.GetOutput(typeof(Stream));
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var canonical = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("xmlns:cac=", canonical);
        var value = Convert.FromBase64String(x.Text("//ds:SignatureValue"));
        using var rsa = cert.GetRSAPublicKey()!;
        Assert.True(rsa.VerifyData(ms.ToArray(), value, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
    }

    [Fact(DisplayName = "A signed XML altered afterwards no longer verifies (SUNAT error 2335)")]
    public void Altered_signed_xml_does_not_verify()
    {
        using var cert = TestCertificate();
        var signed = new XmlDsigSigner(cert).Sign(_generator.Generate(Examples.Cash42()));
        var tampered = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(signed.Xml).Replace(">79.00<", ">7.90<"));
        Assert.False(XmlDsigSigner.Verify(tampered));
    }

    [Fact(DisplayName = "SHA-256 option signs with rsa-sha256 and also verifies")]
    public void Sha256_option()
    {
        using var cert = TestCertificate();
        var signed = new XmlDsigSigner(cert, useSha256: true).Sign(_generator.Generate(Examples.Cash42()));
        Assert.Equal("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", new Xml(signed.Xml).Attr("//ds:SignatureMethod", "Algorithm"));
        Assert.True(XmlDsigSigner.Verify(signed.Xml));
    }
}
