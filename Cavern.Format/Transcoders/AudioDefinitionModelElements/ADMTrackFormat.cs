using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Name, format, and reference information of a track.
    /// </summary>
    public class ADMTrackFormat : TaggedADMElement {
        /// <summary>
        /// Coding of the track.
        /// </summary>
        public ADMTrackCodec Format { get; set; }

        /// <summary>
        /// Referenced stream format by ID.
        /// </summary>
        public string StreamFormat { get; set; }

        /// <summary>
        /// Name, format, and reference information of a track.
        /// </summary>
        public ADMTrackFormat(string id, string name, ADMTrackCodec format, string streamFormat) {
            ID = id;
            Name = name;
            Format = format;
            StreamFormat = streamFormat;
        }

        /// <summary>
        /// An ADM track format parsed from the corresponding XML element.
        /// </summary>
        public ADMTrackFormat(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override XElement Serialize(XNamespace ns) {
            return new XElement(ns + ADMTags.trackFormatTag,
                new XAttribute(ADMTags.trackFormatIDAttribute, ID),
                new XAttribute(ADMTags.trackFormatNameAttribute, Name),
                new XAttribute(ADMTags.formatDefinitionAttribute, Format),
                new XAttribute(ADMTags.formatLabelAttribute, ((int)Format).ToString("x4")),
                new XElement(ns + ADMTags.streamFormatRefTag, StreamFormat));
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.trackFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.trackFormatNameAttribute);
            Format = (ADMTrackCodec)int.Parse(source.GetAttribute(ADMTags.formatLabelAttribute));
            StreamFormat = source.GetElement(ADMTags.streamFormatRefTag);
        }
    }
}