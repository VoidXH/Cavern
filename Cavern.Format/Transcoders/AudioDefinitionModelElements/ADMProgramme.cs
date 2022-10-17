using System;
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
        /// Length of the program in seconds.
        /// </summary>
        readonly double length;

        /// <summary>
        /// ID references of contained <see cref="ADMObject"/>s.
        /// </summary>
        public List<string> Contents { get; set; }

        /// <summary>
        /// Constructs a program of <paramref name="length"/> in seconds.
        /// </summary>
        public ADMProgramme(string id, string name, double length) : base(id, name) => this.length = length;

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
            writer.WriteAttributeString(ADMTags.startAttribute, new TimeSpan().GetTimestamp());
            writer.WriteAttributeString(ADMTags.programEndAttribute, TimeSpan.FromSeconds(length).GetTimestamp());
            SerializeStrings(Contents, writer, ADMTags.contentRefTag);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.programIDAttribute);
            Name = source.GetAttribute(ADMTags.programNameAttribute);
            Contents = ParseStrings(source, ADMTags.contentRefTag);
        }
    }
}