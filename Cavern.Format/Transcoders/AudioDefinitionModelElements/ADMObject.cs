using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a single ADM object with multiple possible tracks.
    /// </summary>
    public sealed class ADMObject : TaggedADMElement {
        /// <summary>
        /// Start of the object's existence.
        /// </summary>
        public ADMTimeSpan Offset { get; set; }

        /// <summary>
        /// Length of the object's existence.
        /// </summary>
        public ADMTimeSpan Length { get; set; }

        /// <summary>
        /// Referenced position/movement container by ID.
        /// </summary>
        public string PackFormat { get; set; }

        /// <summary>
        /// References by ID to coding information of referenced audio data through the <see cref="PackFormat"/>.
        /// </summary>
        public List<string> Tracks { get; set; }

        /// <summary>
        /// Contains a single ADM object with multiple possible tracks.
        /// </summary>
        public ADMObject(string id, string name, ADMTimeSpan offset, ADMTimeSpan length, string packFormat) : base(id, name) {
            Offset = offset;
            Length = length;
            PackFormat = packFormat;
        }

        /// <summary>
        /// Constructs an object from an XML element.
        /// </summary>
        public ADMObject(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override void Serialize(XmlWriter writer) {
            writer.WriteStartElement(ADMTags.objectTag);
            writer.WriteAttributeString(ADMTags.objectIDAttribute, ID);
            writer.WriteAttributeString(ADMTags.objectNameAttribute, Name);
            writer.WriteAttributeString(ADMTags.startAttribute, Offset.ToString());
            writer.WriteAttributeString(ADMTags.durationAttribute, Length.ToString());
            writer.WriteElementString(ADMTags.packFormatRefTag, PackFormat);
            SerializeStrings(Tracks, writer, ADMTags.trackRefTag);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.objectIDAttribute);
            Name = source.GetAttribute(ADMTags.objectNameAttribute);
            Offset = ParseTimestamp(source.Attribute(ADMTags.startAttribute));
            Length = ParseTimestamp(source.Attribute(ADMTags.durationAttribute));
            PackFormat = source.GetElement(ADMTags.packFormatRefTag);
            Tracks = ParseStrings(source, ADMTags.trackRefTag);
        }
    }
}