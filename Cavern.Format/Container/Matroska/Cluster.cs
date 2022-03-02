using System.IO;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Stream data block in a Matroska file.
    /// </summary>
    internal class Cluster {
        /// <summary>
        /// Absolute timestamp of the cluster, expressed in Segment Ticks which is based on TimestampScale.
        /// </summary>
        public long TimeStamp { get; private set; }

        /// <summary>
        /// Parse metadata from a cluster.
        /// </summary>
        public Cluster(BinaryReader reader, MatroskaTree source) {
            TimeStamp = source.GetChildValue(reader, MatroskaTree.Segment_Cluster_Timestamp);
        }
    }
}