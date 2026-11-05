using System.Xml;
using System.Xml.Schema;
using Abc.Invoicing.Application;

namespace Abc.Invoicing.Infrastructure.Ubl;

/// <summary>
/// Adapter of <see cref="IXsdValidator"/> (FR-08, ADR 0003): validates against the official OASIS UBL 2.1 XSDs that
/// travel with the build (xsd/ubl-2.1, hash manifest in the repository). The schema set is compiled once.
/// </summary>
public sealed class OasisXsdValidator : IXsdValidator
{
    private static readonly Lazy<XmlSchemaSet> InvoiceSchema = new(() => Load("maindoc/UBL-Invoice-2.1.xsd"));

    public static string SchemaFolder => Path.Combine(AppContext.BaseDirectory, "xsd", "ubl-2.1");

    private static XmlSchemaSet Load(string relativePath)
    {
        var set = new XmlSchemaSet { XmlResolver = new LocalOnlyResolver(SchemaFolder) };
        // UBL-xmldsig-core-schema-2.1.xsd (copy of the W3C schema) declares entities in an internal DTD subset.
        // It is added first with DTD parsing enabled but no resolver, so nothing is ever fetched from outside.
        var dsig = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
        using (var r = XmlReader.Create(Path.Combine(SchemaFolder, "common", "UBL-xmldsig-core-schema-2.1.xsd"), dsig))
            set.Add(null, r);
        using (var reader = XmlReader.Create(Path.Combine(SchemaFolder, relativePath), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            set.Add(null, reader);
        set.Compile();
        return set;
    }

    /// <summary>Resolves schema imports only to files inside the local xsd folder (no network, no other paths).</summary>
    private sealed class LocalOnlyResolver(string root) : XmlUrlResolver
    {
        private readonly string _root = Path.GetFullPath(root);

        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (!absoluteUri.IsFile || !Path.GetFullPath(absoluteUri.LocalPath).StartsWith(_root, StringComparison.Ordinal))
                throw new XmlException($"Schema outside the local xsd folder refused: {absoluteUri}");
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
    }

    public IReadOnlyList<string> Validate(byte[] xml)
    {
        var errors = new List<string>();
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = InvoiceSchema.Value,
            DtdProcessing = DtdProcessing.Prohibit,
        };
        // Only errors count. Warnings are "no schema information" notices for the lax xsd:any of ext:ExtensionContent
        // (signature placeholder), which the XSD explicitly allows.
        settings.ValidationEventHandler += (_, e) =>
        {
            if (e.Severity == XmlSeverityType.Error)
                errors.Add($"line {e.Exception?.LineNumber}: {e.Message}");
        };
        try
        {
            using var reader = XmlReader.Create(new MemoryStream(xml), settings);
            while (reader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add($"Not well-formed: {ex.Message}");
        }
        return errors;
    }
}
