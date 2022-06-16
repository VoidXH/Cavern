using Cavern.Format.Common;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Track data for a <see cref="MatroskaReader"/>, containing read positions.
    /// </summary>
    internal class MatroskaTrack : Track {
        /// <summary>
        /// Currently read cluster.
        /// </summary>
        internal int lastCluster;

        /// <summary>
        /// Last read block in the current cluster.
        /// </summary>
        internal int lastBlock;

        /// <summary>
        /// Create a track to be placed in the list of a container's tracks.
        /// </summary>
        /// <param name="source">The container containing this track.</param>
        /// <param name="trackNumber">The position of the track in the container's list of tracks.</param>
        /// <remarks>The <paramref name="trackNumber"/> required for reading from the container.</remarks>
        internal MatroskaTrack(ContainerReader source, int trackNumber) : base(source, trackNumber) { }
    }
}