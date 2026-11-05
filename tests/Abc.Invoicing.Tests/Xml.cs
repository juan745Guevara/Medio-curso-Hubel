using System.Globalization;
using System.Xml;

namespace Abc.Invoicing.Tests;

/// <summary>Small XPath helper over a generated UBL document.</summary>
public sealed class Xml
{
    private readonly XmlDocument _doc = new() { PreserveWhitespace = true };
    private readonly XmlNamespaceManager _ns;

    public Xml(byte[] bytes)
    {
        _doc.Load(new MemoryStream(bytes));
        _ns = new XmlNamespaceManager(_doc.NameTable);
        _ns.AddNamespace("inv", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        _ns.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        _ns.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
        _ns.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
        _ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
    }

    public XmlDocument Document => _doc;

    public XmlElement? Node(string xpath) => _doc.SelectSingleNode(xpath, _ns) as XmlElement;

    public IReadOnlyList<XmlElement> Nodes(string xpath) => _doc.SelectNodes(xpath, _ns)!.OfType<XmlElement>().ToList();

    public string Text(string xpath) => Node(xpath)?.InnerText ?? throw new Xunit.Sdk.XunitException($"No node {xpath}");

    public decimal Dec(string xpath) => decimal.Parse(Text(xpath), CultureInfo.InvariantCulture);

    public string Attr(string xpath, string attribute) => Node(xpath)?.GetAttribute(attribute) ?? throw new Xunit.Sdk.XunitException($"No node {xpath}");

    public void Remove(string xpath)
    {
        var n = Node(xpath)!;
        n.ParentNode!.RemoveChild(n);
    }

    public void Set(string xpath, string value) => Node(xpath)!.InnerText = value;

    public void SetAttr(string xpath, string attribute, string value) => Node(xpath)!.SetAttribute(attribute, value);

    public byte[] Bytes()
    {
        using var ms = new MemoryStream();
        _doc.Save(ms);
        return ms.ToArray();
    }
}
