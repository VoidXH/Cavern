using System.Collections.Generic;
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
        /// All blocks of the cluster, in order.
        /// </summary>
        public IReadOnlyList<Block> Blocks => blocks;

        /// <summary>
        /// All blocks of the cluster, in order.
        /// </summary>
        readonly Block[] blocks;

        /// <summary>
        /// Parse metadata from a cluster.
        /// </summary>
        public Cluster(BinaryReader reader, MatroskaTree source) {
            TimeStamp = source.GetChildValue(reader, MatroskaTree.Segment_Cluster_Timestamp);

            // Block groups (Matroska v1)
            List<MatroskaTree> blocksSource =
                source.GetChildrenByPath(MatroskaTree.Segment_Cluster_BlockGroup, MatroskaTree.Segment_Cluster_BlockGroup_Block);
            if (blocksSource.Count != 0) {
                blocks = new Block[blocksSource.Count];
                for (int i = 0, c = blocksSource.Count; i < c; ++i)
                    blocks[i] = new Block(reader, blocksSource[i]);
            }

            // Simple blocks (Matroska v2)
            else {
                MatroskaTree[] simpleSource = source.GetChildren(MatroskaTree.Segment_Cluster_SimpleBlock);
                blocks = new Block[simpleSource.Length];
                for (int i = 0; i < simpleSource.Length; ++i)
                    blocks[i] = new Block(reader, simpleSource[i]);
            }
        }
    }
}