using Cavern.Format.Container;

namespace Cavern.Format.Common {
    /// <summary>
    /// Metadata and continuous reading handler of a track.
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

        /// <summary>
        /// The container containing this track.
        /// </summary>
        public ContainerReader Source { get; private set; }

        /// <summary>
        /// The position of the track in the container's list of tracks.
        /// </summary>
        int trackNumber;

        /// <summary>
        /// Create a track to be placed in the list of a container's tracks after it's set up with
        /// <see cref="Override(ContainerReader, int)"/>.
        /// </summary>
        internal Track() { }

        /// <summary>
        /// Create a track to be placed in the list of a container's tracks.
        /// </summary>
        /// <param name="source">The container containing this track.</param>
        /// <param name="trackNumber">The position of the track in the container's list of tracks.</param>
        /// <remarks>The <paramref name="trackNumber"/> required for reading from the container.</remarks>
        internal Track(ContainerReader source, int trackNumber) {
            Source = source;
            this.trackNumber = trackNumber;
        }

        /// <summary>
        /// Late-init the <see cref="Source"/> and <see cref="trackNumber"/>.
        /// </summary>
        internal void Override(ContainerReader source, int trackNumber) {
            Source = source;
            this.trackNumber = trackNumber;
        }

        /// <summary>
        /// Continue reading the track.
        /// </summary>
        public byte[] ReadNextBlock() => Source.ReadNextBlock(trackNumber);

        /// <summary>
        /// Returns if the next block can be completely decoded by itself.
        /// </summary>
        public bool IsNextBlockKeyframe() => Source.IsNextBlockKeyframe(trackNumber);

        /// <summary>
        /// Get the block's offset in seconds.
        /// </summary>
        public double GetNextBlockOffset() => Source.GetNextBlockOffset(trackNumber);
    }
}