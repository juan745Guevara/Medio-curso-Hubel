using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Abc.Ubl
{
    /// <summary>Serializes the generated UBL classes exactly as SUNAT expects them (ADR 0005 §3).</summary>
    public static class UblSerializer
    {
        public const string Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        public const string Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        public const string Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
        public const string Ds = "http://www.w3.org/2000/09/xmldsig#";

        private static readonly ConcurrentDictionary<Type, XmlSerializer> Cache = new();

        private static XmlSerializer For(Type t) => Cache.GetOrAdd(t, type =>
        {
            // cbc:*Time → hh:mm:ss (see Customizations/TimeType.cs)
            var overrides = new XmlAttributeOverrides();
            overrides.Add(typeof(Udt.TimeType), nameof(Udt.TimeType.Value), new XmlAttributes { XmlIgnore = true });
            overrides.Add(typeof(Udt.TimeType), nameof(Udt.TimeType.Text), new XmlAttributes { XmlText = new XmlTextAttribute() });
            return new XmlSerializer(type, overrides);
        });

        /// <summary>UTF-8 without BOM, no indentation (a signed XML is never reformatted), fixed prefixes.</summary>
        public static byte[] Serialize<T>(T document, string defaultNamespace)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, defaultNamespace);
            ns.Add("cac", Cac);
            ns.Add("cbc", Cbc);
            ns.Add("ext", Ext);
            ns.Add("ds", Ds);
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, OmitXmlDeclaration = false };
            using var ms = new MemoryStream();
            using (var w = XmlWriter.Create(ms, settings))
            {
                For(typeof(T)).Serialize(w, document, ns);
            }
            return ms.ToArray();
        }

        public static T Deserialize<T>(byte[] xml)
        {
            using var ms = new MemoryStream(xml);
            return (T)For(typeof(T)).Deserialize(ms)!;
        }
    }
}
