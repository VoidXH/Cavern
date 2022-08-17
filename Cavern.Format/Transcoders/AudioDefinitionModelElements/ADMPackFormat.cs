using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains position/movement data for each channel contained in an object.
    /// </summary>
    public class ADMPackFormat : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Type of the contained tracks (channels, objects, etc.).
        /// </summary>
        public ADMPackType Type { get; set; }

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
                new XAttribute(ADMTags.packFormatTypeStringAttribute, Type),
                new XAttribute(ADMTags.packFormatTypeAttribute, ((int)Type).ToString("X4")));
            parent.Add(root);
        }
    }
}