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
    }
}