using Cavern.Format.Utilities;
using System.Xml.Linq;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Coding information of an ADM's track.
    /// </summary>
    public sealed class ADMTrack : IXDocumentSerializable {
        /// <summary>
        /// Unique identifier of the track.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Bit depth of the track.
        /// </summary>
        public BitDepth Bits { get; private set; }

        /// <summary>
        /// Sampling rate of the track.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Reference to track format by ID.
        /// </summary>
        public string TrackFormat { get; private set; }

        /// <summary>
        /// Reference to pack format by ID.
        /// </summary>
        public string PackFormat { get; private set; }

        /// <summary>
        /// Coding information of an ADM's track.
        /// </summary>
        public ADMTrack(string id, BitDepth bitDepth, int sampleRate, string trackFormat, string packFormat) {
            ID = id;
            Bits = bitDepth;
            SampleRate = sampleRate;
            TrackFormat = trackFormat;
            PackFormat = packFormat;
        }

        /// <summary>
        /// Constructs a track from an XML element.
        /// </summary>
        public ADMTrack(XElement source) => Deserialize(source);

        /// <summary>
        /// Create an XML element about this object.
        /// </summary>
        public XElement Serialize(XNamespace ns) {
            return new XElement(ns + ADMTags.trackTag,
                new XAttribute(ADMTags.trackIDAttribute, ID),
                new XAttribute(ADMTags.trackBitDepthAttribute, (int)Bits),
                new XAttribute(ADMTags.trackSampleRateAttribute, SampleRate),
                new XElement(ns + ADMTags.trackFormatRefTag, TrackFormat),
                new XElement(ns + ADMTags.packFormatRefTag, PackFormat));
        }

        /// <summary>
        /// Read the values of an XML element into this object.
        /// </summary>
        public void Deserialize(XElement source) {
            ID = source.GetAttribute(ADMTags.trackIDAttribute);
            Bits = (BitDepth)int.Parse(source.GetAttribute(ADMTags.trackBitDepthAttribute));
            SampleRate = int.Parse(source.GetAttribute(ADMTags.trackSampleRateAttribute));
            TrackFormat = source.GetElement(ADMTags.trackFormatRefTag);
            PackFormat = source.GetElement(ADMTags.packFormatRefTag);
        }
    }
}