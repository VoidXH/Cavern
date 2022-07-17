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
        /// Element to read the cluster from.
        /// </summary>
        readonly MatroskaTree source;

        /// <summary>
        /// All blocks of the cluster, in order.
        /// </summary>
        List<Block> blocks;

        /// <summary>
        /// Parse metadata from a cluster.
        /// </summary>
        public Cluster(Stream reader, MatroskaTree source) {
            this.source = source;
            TimeStamp = source.GetChildValue(reader, MatroskaTree.Segment_Cluster_Timestamp);
        }

        /// <summary>
        /// All blocks of the cluster, in order.
        /// </summary>
        public IReadOnlyList<Block> GetBlocks(Stream reader) {
            if (blocks != null) {
                return blocks;
            }
            MemoryStream stream = new MemoryStream(source.GetRawData(reader));
            blocks = new List<Block>();
            long end = stream.Length - 2; // Safety barrier, a byte might remain, but a 2 byte child is impossible
            while (stream.Position < end) {
                MatroskaTree child = new MatroskaTree(stream);
                long continueFrom = stream.Position;

                // Block groups (Matroska v1)
                if (child.Tag == MatroskaTree.Segment_Cluster_BlockGroup) {
                    MatroskaTree[] blocksHere = child.GetChildren(stream, MatroskaTree.Segment_Cluster_BlockGroup_Block);
                    for (int i = 0; i < blocksHere.Length; ++i) {
                        blocks.Add(new Block(stream, blocksHere[i]));
                    }
                }

                // Simple blocks (Matroska v2)
                else if (child.Tag == MatroskaTree.Segment_Cluster_SimpleBlock) {
                    blocks.Add(new Block(stream, child));
                }

                stream.Position = continueFrom;
            }

            return blocks;
        }
    }
}