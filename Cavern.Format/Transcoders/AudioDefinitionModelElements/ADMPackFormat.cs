using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains position/movement data for each channel contained in an object.
    /// </summary>
    public sealed class ADMPackFormat : TaggedADMElement {
        /// <summary>
        /// Type of the contained tracks (channels, objects, etc.).
        /// </summary>
        public ADMPackType Type { get; private set; }

        /// <summary>
        /// Positional data of the channels related to this object.
        /// </summary>
        public List<string> ChannelFormats { get; set; }

        /// <summary>
        /// Contains position/movement data for each channel contained in an object.
        /// </summary>
        public ADMPackFormat(string id, string name, ADMPackType type) {
            ID = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Constructs a pack format from an XML element.
        /// </summary>
        public ADMPackFormat(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override void Serialize(XmlWriter writer) {
            writer.WriteStartElement(ADMTags.packFormatTag);
            writer.WriteAttributeString(ADMTags.packFormatIDAttribute, ID);
            writer.WriteAttributeString(ADMTags.packFormatNameAttribute, Name);
            writer.WriteAttributeString(ADMTags.typeStringAttribute, Type.ToString());
            writer.WriteAttributeString(ADMTags.typeAttribute, ((int)Type).ToString("x4"));
            SerializeStrings(ChannelFormats, writer, ADMTags.channelFormatRefTag);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.packFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.packFormatNameAttribute);
            Type = (ADMPackType)int.Parse(source.GetAttribute(ADMTags.typeAttribute));
            ChannelFormats = ParseStrings(source, ADMTags.channelFormatRefTag);
        }
    }
}