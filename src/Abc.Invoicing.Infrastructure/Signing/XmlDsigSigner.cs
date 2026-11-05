using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Abc.Invoicing.Application;
using Abc.Invoicing.Infrastructure.Ubl;
using DsSignedXml = System.Security.Cryptography.Xml.SignedXml;
using DsKeyInfo = System.Security.Cryptography.Xml.KeyInfo;
using DsKeyInfoX509Data = System.Security.Cryptography.Xml.KeyInfoX509Data;
using DsReference = System.Security.Cryptography.Xml.Reference;
using DsEnveloped = System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform;

namespace Abc.Invoicing.Infrastructure.Signing;

/// <summary>
/// Adapter of <see cref="IXmlSigner"/> (FR-09): XMLDSig enveloped, inline C14N, signature placed where the generator
/// left the placeholder (ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent), Id "SignABC" as referenced by
/// cac:Signature. The certificate comes from the server's secret store (ADR 0006); tests use a self-signed one.
/// SUNAT's reference libraries sign with RSA-SHA1 (research 001); SHA-256 is available as an option.
/// </summary>
public sealed class XmlDsigSigner(X509Certificate2 certificate, bool useSha256 = false) : IXmlSigner
{
    public SignedXml Sign(byte[] xml)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.Load(new MemoryStream(xml));
        var placeholder = doc.GetElementsByTagName(Ubl21InvoiceGenerator.PlaceholderName, Ubl21InvoiceGenerator.PlaceholderNamespace)
            .OfType<XmlElement>().SingleOrDefault()
            ?? throw new InvalidOperationException("The XML has no signature placeholder in ext:ExtensionContent");
        // The digest is computed over the document as it will look without the ds:Signature (the enveloped
        // transform removes only the signature when verifying), so the placeholder leaves before signing.
        var slot = (XmlElement)placeholder.ParentNode!;
        slot.RemoveChild(placeholder);

        var key = certificate.GetRSAPrivateKey() ?? throw new InvalidOperationException("The certificate has no RSA private key");
        var signed = new DsSignedXml(doc) { SigningKey = key };
        signed.SignedInfo!.CanonicalizationMethod = DsSignedXml.XmlDsigC14NTransformUrl;
        signed.SignedInfo.SignatureMethod = useSha256 ? DsSignedXml.XmlDsigRSASHA256Url : DsSignedXml.XmlDsigRSASHA1Url;
        var reference = new DsReference(string.Empty) { DigestMethod = useSha256 ? DsSignedXml.XmlDsigSHA256Url : DsSignedXml.XmlDsigSHA1Url };
        reference.AddTransform(new DsEnveloped());
        signed.AddReference(reference);
        var keyInfo = new DsKeyInfo();
        keyInfo.AddClause(new DsKeyInfoX509Data(certificate));
        signed.KeyInfo = keyInfo;
        signed.ComputeSignature();

        // Written with the ds: prefix the Invoice root already declares (as SUNAT's samples and Greenter do) instead
        // of redeclaring the default namespace inside the document.
        var signature = WithDsPrefix(doc, signed.GetXml());
        signature.SetAttribute("Id", "SignABC");
        slot.AppendChild(signature);

        // SignedXml signs the SignedInfo canonicalized on its own, but a verifier (SUNAT, Apache Santuario, signxml)
        // canonicalizes it where it lives, with the namespaces inherited from Invoice (cac, cbc, ext, ds…). Inclusive
        // C14N renders those on the apex element, so the SignatureValue is recomputed over the in-place form.
        var signedInfo = (XmlElement)signature.GetElementsByTagName("SignedInfo", DsNs)[0]!;
        var c14nBytes = CanonicalInPlace(signedInfo);
        var value = key.SignData(c14nBytes, useSha256 ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        signature.GetElementsByTagName("SignatureValue", DsNs)[0]!.InnerText = Convert.ToBase64String(value);

        using var ms = new MemoryStream();
        using (var w = XmlWriter.Create(ms, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false }))
            doc.Save(w);
        var digest = signature.GetElementsByTagName("DigestValue", DsNs)[0]!.InnerText;
        return new SignedXml(ms.ToArray(), digest);
    }

    private const string DsNs = "http://www.w3.org/2000/09/xmldsig#";

    private static XmlElement WithDsPrefix(XmlDocument doc, XmlElement source)
    {
        var copy = doc.CreateElement("ds", source.LocalName, DsNs);
        foreach (XmlAttribute a in source.Attributes)
            if (a.Name != "xmlns" && a.Prefix != "xmlns")
                copy.SetAttribute(a.LocalName, a.NamespaceURI, a.Value);
        foreach (XmlNode child in source.ChildNodes)
            copy.AppendChild(child is XmlElement e ? WithDsPrefix(doc, e) : doc.ImportNode(child, true));
        return copy;
    }
    private const string XmlnsNs = "http://www.w3.org/2000/xmlns/";

    /// <summary>Inclusive C14N of an element as it is in its document: in-scope namespace declarations included.</summary>
    private static byte[] CanonicalInPlace(XmlElement element)
    {
        // Same construction System.Security.Cryptography.Xml uses when it verifies (GetPropagatedAttributes +
        // AddNamespaces): a copy of the element carrying every namespace declaration in scope from its ancestors.
        var tmp = new XmlDocument { PreserveWhitespace = true };
        var copy = (XmlElement)tmp.ImportNode(element, true);
        tmp.AppendChild(copy);
        for (var n = element.ParentNode as XmlElement; n is not null; n = n.ParentNode as XmlElement)
            foreach (XmlAttribute a in n.Attributes)
                // A default namespace on an ancestor never applies to SignedInfo: its own (unprefixed) element
                // namespace, xmldsig, is the default in scope there.
                if (a.Name == "xmlns" && string.IsNullOrEmpty(element.Prefix))
                    continue;
                else if ((a.Name == "xmlns" || a.Prefix == "xmlns") && !copy.HasAttribute(a.Name))
                {
                    var ns = tmp.CreateAttribute(a.Name);
                    ns.Value = a.Value;
                    copy.SetAttributeNode(ns);
                }
        var c14n = new System.Security.Cryptography.Xml.XmlDsigC14NTransform();
        c14n.LoadInput(tmp);
        using var output = (Stream)c14n.GetOutput(typeof(Stream));
        using var ms = new MemoryStream();
        output.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Verifies the enveloped signature of a signed UBL document with the certificate it carries (AC-16).</summary>
    public static bool Verify(byte[] signedXml)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.Load(new MemoryStream(signedXml));
        var sig = doc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").OfType<XmlElement>().Single();
        var check = new DsSignedXml(doc);
        check.LoadXml(sig);
        var certText = sig.GetElementsByTagName("X509Certificate", "http://www.w3.org/2000/09/xmldsig#")[0]!.InnerText;
        using var cert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certText));
        return check.CheckSignature(cert, verifySignatureOnly: true);
    }
}
