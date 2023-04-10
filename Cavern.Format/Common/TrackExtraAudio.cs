using System.IO;

using Cavern.Format.Container.Matroska;

namespace Cavern.Format.Common {
    /// <summary>
    /// Audio track metadata.
    /// </summary>
    public class TrackExtraAudio : TrackExtra {
        /// <summary>
        /// Sampling frequency of the track in Hertz.
        /// </summary>
        public double SampleRate { get; set; }

        /// <summary>
        /// Number of discrete channels for channel-based (down)mixes.
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// Audio sample size in bits.
        /// </summary>
        public BitDepth Bits { get; set; }

        /// <summary>
        /// An empty audio track metadata.
        /// </summary>
        public TrackExtraAudio() { }

        /// <summary>
        /// Parse audio metadata from a Matroska track's audio metadata node.
        /// </summary>
        internal TrackExtraAudio(Stream reader, MatroskaTree audioMeta) {
            SampleRate = audioMeta.GetChildFloatBE(reader, MatroskaTree.Segment_Tracks_TrackEntry_Audio_SamplingFrequency);
            ChannelCount = (int)audioMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels);
            Bits = (BitDepth)audioMeta.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_Audio_BitDepth);
        }
    }
}