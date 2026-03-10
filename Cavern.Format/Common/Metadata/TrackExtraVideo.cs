namespace Cavern.Format.Common.Metadata {
    /// <summary>
    /// Video track metadata.
    /// </summary>
    public class TrackExtraVideo : TrackExtra {
        /// <summary>
        /// Uncropped width of a video frame.
        /// </summary>
        public uint Width { get; set; }

        /// <summary>
        /// Uncropped height of a video frame.
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// Frame update frequency.
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// Color-related properties, null if none is defined.
        /// </summary>
        public ColorMetadata ColorMetadata { get; set; }

        /// <summary>
        /// An optional codec header.
        /// </summary>
        public byte[] PrivateData { get; set; }

        /// <summary>
        /// An empty video track metadata.
        /// </summary>
        public TrackExtraVideo() { }
    }
}
