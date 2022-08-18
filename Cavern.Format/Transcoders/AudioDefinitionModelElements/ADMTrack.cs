using Cavern.Format.Utilities;
using System.Xml.Linq;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Coding information of an ADM's track.
    /// </summary>
    public class ADMTrack : IXDocumentSerializable {
        /// <summary>
        /// Unique identifier of the track.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Bit depth of the track.
        /// </summary>
        public BitDepth BitDepth { get; private set; }

        /// <summary>
        /// Sampling rate of the track.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Parent object that this pack format describes.
        /// </summary>
        public ADMObject Object { get; private set; }

        /// <summary>
        /// Coding information of an ADM's track.
        /// </summary>
        public ADMTrack(string id, BitDepth bitDepth, int sampleRate, ADMObject obj) {
            ID = id;
            BitDepth = bitDepth;
            SampleRate = sampleRate;
            Object = obj;
        }

        /// <summary>
        /// Create an XML element added to a <paramref name="parent"/>.
        /// </summary>
        public void Serialize(XElement parent) {
            string trackFormatID = $"AT_{Object.PackFormat.ID[3..]}_01";
            string streamFormatID = $"AS_{Object.PackFormat.ID[3..]}";
            parent.Add(new XElement(parent.Name.Namespace + ADMTags.trackTag,
                new XAttribute(ADMTags.trackIDAttribute, ID),
                new XAttribute(ADMTags.trackBitDepthAttribute, (int)BitDepth),
                new XAttribute(ADMTags.trackSampleRateAttribute, SampleRate),
                new XElement(parent.Name.Namespace + ADMTags.packFormatRefTag, Object.PackFormat.ID),
                new XElement(parent.Name.Namespace + ADMTags.trackFormatRefTag, trackFormatID)));
            parent.Add(new XElement(parent.Name.Namespace + ADMTags.trackFormatTag,
                new XAttribute(ADMTags.trackFormatIDAttribute, trackFormatID),
                new XAttribute(ADMTags.trackFormatNameAttribute, "Cavern_Obj_" + Object.PackFormat.ID[7..]),
                new XElement(parent.Name.Namespace + ADMTags.streamFormatRefTag, streamFormatID)));
            parent.Add(new XElement(parent.Name.Namespace + ADMTags.streamFormatTag,
                new XAttribute(ADMTags.streamFormatIDAttribute, streamFormatID),
                new XAttribute(ADMTags.streamFormatNameAttribute, "PCM_Cavern_Obj_" + Object.PackFormat.ID[7..]),
                new XElement(parent.Name.Namespace + ADMTags.channelFormatRefTag, Object.PackFormat.ChannelFormats[0].ID),
                new XElement(parent.Name.Namespace + ADMTags.packFormatRefTag, Object.PackFormat.ID),
                new XElement(parent.Name.Namespace + ADMTags.trackFormatRefTag, trackFormatID)));
        }
    }
}