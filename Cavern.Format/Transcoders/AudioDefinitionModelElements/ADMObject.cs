using System;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a single ADM object with multiple possible tracks.
    /// </summary>
    public class ADMObject : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Start of the object's existence in seconds.
        /// </summary>
        readonly double offset;

        /// <summary>
        /// Length of the object's existence in seconds.
        /// </summary>
        readonly double length;

        /// <summary>
        /// Position/movement data for each contained channel.
        /// </summary>
        public ADMPackFormat PackFormat { get; set; }

        /// <summary>
        /// Creates an object of <paramref name="length"/> in seconds.
        /// </summary>
        public ADMObject(string id, string name, double offset, double length) {
            ID = id;
            Name = name;
            this.offset = offset;
            this.length = length;
        }

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            XElement root = new XElement(parent.Name.Namespace + ADMTags.objectTag,
                new XAttribute(ADMTags.objectIDAttribute, ID),
                new XAttribute(ADMTags.objectNameAttribute, Name),
                new XAttribute(ADMTags.startAttribute, TimeSpan.FromSeconds(offset).GetTimestamp()),
                new XAttribute(ADMTags.durationAttribute, TimeSpan.FromSeconds(length).GetTimestamp()),
                new XElement(parent.Name.Namespace + ADMTags.packFormatRefTag, PackFormat.ID));
            parent.Add(root);
            PackFormat.Serialize(parent);
        }
    }
}