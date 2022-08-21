using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Contains a single ADM object with multiple possible tracks.
    /// </summary>
    public sealed class ADMObject : TaggedADMElement, IXDocumentSerializable {
        /// <summary>
        /// Start of the object's existence.
        /// </summary>
        public TimeSpan Offset { get; set; }

        /// <summary>
        /// Length of the object's existence.
        /// </summary>
        public TimeSpan Length { get; set; }

        /// <summary>
        /// Position/movement data for each contained channel.
        /// </summary>
        public ADMPackFormat PackFormat { get; set; }

        /// <summary>
        /// Coding information of referenced audio data.
        /// </summary>
        public List<ADMTrack> Tracks { get; set; }

        /// <summary>
        /// Contains a single ADM object with multiple possible tracks.
        /// </summary>
        public ADMObject(string id, string name) {
            ID = id;
            Name = name;
        }

        /// <summary>
        /// Constructs an object from an XML element.
        /// </summary>
        public ADMObject(XElement source) => Deserialize(source);

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            parent.Add(new XElement(parent.Name.Namespace + ADMTags.objectTag,
                new XAttribute(ADMTags.objectIDAttribute, ID),
                new XAttribute(ADMTags.objectNameAttribute, Name),
                new XAttribute(ADMTags.startAttribute, Offset.GetTimestamp()),
                new XAttribute(ADMTags.durationAttribute, Length.GetTimestamp()),
                new XElement(parent.Name.Namespace + ADMTags.packFormatRefTag, PackFormat.ID)));
            PackFormat.Serialize(parent);
            for (int i = 0, c = Tracks.Count; i < c; i++) {
                parent.Add(new XElement(parent.Name.Namespace + ADMTags.trackRefTag, Tracks[i].ID));
                Tracks[i].Serialize(parent);
            }
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.objectIDAttribute);
            Name = source.GetAttribute(ADMTags.objectNameAttribute);
            Offset = ParseTimestamp(source.Attribute(ADMTags.startAttribute));
            Length = ParseTimestamp(source.Attribute(ADMTags.durationAttribute));
            // TODO: pack format, track refs
        }
    }
}