namespace Cavern.Format.Common {
    /// <summary>
    /// Contains meatadata of a track in a container.
    /// </summary>
    public class Track {
        /// <summary>
        /// Identifier of the track.
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// Name of the track.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Language code of the track.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Codec of the track.
        /// </summary>
        public Codec Format { get; set; }

        /// <summary>
        /// Additional metadata depending on the content type.
        /// </summary>
        public TrackExtra Extra { get; set; }
    }
}