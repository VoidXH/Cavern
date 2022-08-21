using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains position/movement data for each channel contained in an object.
    /// </summary>
    public sealed class ADMPackFormat : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Type of the contained tracks (channels, objects, etc.).
        /// </summary>
        public ADMPackType Type { get; private set; }

        /// <summary>
        /// Parent object that this pack format describes.
        /// </summary>
        public ADMObject Object { get; private set; }

        /// <summary>
        /// Contains position/movement data for each channel contained in an object.
        /// </summary>
        public ADMPackFormat(string id, string name, ADMPackType type, ADMObject parent) {
            ID = id;
            Name = name;
            Type = type;
            Object = parent;
            Object.PackFormat = this;
        }

        /// <summary>
        /// Constructs a pack format from an XML element.
        /// </summary>
        public ADMPackFormat(XElement source) => Deserialize(source);

        /// <summary>
        /// Positional data of the channels related to this object.
        /// </summary>
        public List<ADMChannelFormat> ChannelFormats { get; set; }

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            XElement root = new XElement(parent.Name.Namespace + ADMTags.packFormatTag,
                new XAttribute(ADMTags.packFormatIDAttribute, ID),
                new XAttribute(ADMTags.packFormatNameAttribute, Name),
                new XAttribute(ADMTags.typeStringAttribute, Type),
                new XAttribute(ADMTags.typeAttribute, ((int)Type).ToString("x4")));
            parent.Add(root);
            foreach (ADMChannelFormat channel in ChannelFormats) {
                root.Add(new XElement(parent.Name.Namespace + ADMTags.channelFormatRefTag, channel.ID));
                channel.Serialize(parent);
            }
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.packFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.packFormatNameAttribute);
            Type = (ADMPackType)int.Parse(source.GetAttribute(ADMTags.typeAttribute));
        }
    }
}