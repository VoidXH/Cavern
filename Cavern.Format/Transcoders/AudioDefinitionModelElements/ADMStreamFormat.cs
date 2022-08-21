using System.Xml.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Merging of format information elements.
    /// </summary>
    public class ADMStreamFormat : TaggedADMElement {
        /// <summary>
        /// Coding of the track.
        /// </summary>
        public ADMTrackCodec Format { get; set; }

        /// <summary>
        /// Referenced channel format by ID.
        /// </summary>
        public string ChannelFormat { get; set; }

        /// <summary>
        /// Referenced pack format by ID.
        /// </summary>
        public string PackFormat { get; set; }

        /// <summary>
        /// Referenced track format by ID.
        /// </summary>
        public string TrackFormat { get; set; }

        /// <summary>
        /// Merging of format information elements.
        /// </summary>
        public ADMStreamFormat(string id, string name, ADMTrackCodec format,
            string channelFormat, string packFormat, string trackFormat) {
            ID = id;
            Name = name;
            Format = format;
            ChannelFormat = channelFormat;
            PackFormat = packFormat;
            TrackFormat = trackFormat;
        }

        /// <summary>
        /// An ADM track format parsed from the corresponding XML element.
        /// </summary>
        public ADMStreamFormat(XElement source) : base(source) { }

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public override XElement Serialize(XNamespace ns) {
            return new XElement(ns + ADMTags.streamFormatTag,
                new XAttribute(ADMTags.streamFormatIDAttribute, ID),
                new XAttribute(ADMTags.streamFormatNameAttribute, Name),
                new XAttribute(ADMTags.formatDefinitionAttribute, Format),
                new XAttribute(ADMTags.formatLabelAttribute, ((int)Format).ToString("x4")),
                new XElement(ns + ADMTags.channelFormatRefTag, ChannelFormat),
                new XElement(ns + ADMTags.packFormatRefTag, PackFormat),
                new XElement(ns + ADMTags.trackFormatRefTag, TrackFormat));
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public override void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.streamFormatIDAttribute);
            Name = source.GetAttribute(ADMTags.streamFormatNameAttribute);
            Format = (ADMTrackCodec)int.Parse(source.GetAttribute(ADMTags.formatLabelAttribute));
            ChannelFormat = source.GetElement(ADMTags.channelFormatRefTag);
            PackFormat = source.GetElement(ADMTags.packFormatRefTag);
            TrackFormat = source.GetElement(ADMTags.trackFormatRefTag);
        }
    }
}