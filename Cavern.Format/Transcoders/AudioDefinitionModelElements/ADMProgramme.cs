using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Root element of the model's contained program.
    /// </summary>
    public sealed class ADMProgramme : TaggedADMElement {
        /// <summary>
        /// Length of the program.
        /// </summary>
        public ADMTimeSpan Length { get; private set; }

        /// <summary>
        /// ID references of contained <see cref="ADMObject"/>s.
        /// </summary>
        public List<string> Contents { get; set; }

        /// <summary>
        /// Constructs a program of <paramref name="length"/> in seconds.
        /// </summary>
        public ADMProgramme(string id, string name, ADMTimeSpan length) : base(id, name) => Length = length;

        /// <summary>
        /// Constructs a program from an XML element.
        /// </summary>
        public ADMProgramme(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override void Serialize(XmlWriter writer) {
            writer.WriteStartElement(ADMTags.programTag);
            writer.WriteAttributeString(ADMTags.programIDAttribute, ID);
            writer.WriteAttributeString(ADMTags.programNameAttribute, Name);
            writer.WriteAttributeString(ADMTags.startAttribute, ADMTimeSpan.Zero.ToString());
            writer.WriteAttributeString(ADMTags.programEndAttribute, Length.ToString());
            SerializeStrings(Contents, writer, ADMTags.contentRefTag);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.programIDAttribute);
            Name = source.GetAttribute(ADMTags.programNameAttribute);
            Length = ParseTimestamp(source.Attribute(ADMTags.programEndAttribute));
            Contents = ParseStrings(source, ADMTags.contentRefTag);
        }
    }
}