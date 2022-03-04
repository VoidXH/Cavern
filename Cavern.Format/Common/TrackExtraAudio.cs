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
        public BitDepth Depth { get; set; }
    }
}