using System;
using System.Globalization;
using System.Xml.Serialization;

namespace Abc.Ubl.Udt
{
    /// <summary>
    /// SUNAT writes cbc:IssueTime as hh:mm:ss (research 001, matrix row 7). XmlSerializer writes a DateTime with
    /// DataType "time" as "hh:mm:ss.fffffffzzz", which is valid xsd:time but not SUNAT's format, so the serializer
    /// built by <c>UblSerializer</c> ignores <see cref="Value"/> and writes <see cref="Text"/> instead.
    /// This file is hand-written and never overwritten by the generator.
    /// </summary>
    public partial class TimeType
    {
        [XmlIgnore]
        public string Text
        {
            get => Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            set => Value = DateTime.ParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}
