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
        public double Offset { get; set; }

        /// <summary>
        /// Length of the object's existence in seconds.
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// Position/movement data for each contained channel.
        /// </summary>
        public ADMPackFormat PackFormat { get; set; }

        /// <summary>
        /// Coding information of referenced audio data.
        /// </summary>
        public ADMTrack Track { get; set; }

        /// <summary>
        /// Contains a single ADM object with multiple possible tracks.
        /// </summary>
        public ADMObject(string id, string name) {
            ID = id;
            Name = name;
        }

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            parent.Add(new XElement(parent.Name.Namespace + ADMTags.objectTag,
                new XAttribute(ADMTags.objectIDAttribute, ID),
                new XAttribute(ADMTags.objectNameAttribute, Name),
                new XAttribute(ADMTags.startAttribute, TimeSpan.FromSeconds(Offset).GetTimestamp()),
                new XAttribute(ADMTags.durationAttribute, TimeSpan.FromSeconds(Length).GetTimestamp()),
                new XElement(parent.Name.Namespace + ADMTags.packFormatRefTag, PackFormat.ID),
                new XElement(parent.Name.Namespace + ADMTags.trackRefTag, Track.ID)));
            PackFormat.Serialize(parent);
            Track.Serialize(parent);
        }
    }
}