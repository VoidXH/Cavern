using System.Collections.Generic;
using System.IO;

using Cavern.Utilities;

namespace Cavern.Format.Container {
    /// <summary>
    /// Reads EBML, a kind of binary XML format that is used by Matroska.
    /// </summary>
    /// <see href="https://www.matroska.org/files/matroska_file_format_alexander_noe.pdf"/>
    public class MatroskaReader : ContainerReader {
        /// <summary>
        /// Nanoseconds to seconds.
        /// </summary>
        const double nsToS = 1.0 / 1000000000;

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(BinaryReader reader) : base(reader) { ReadSkeleton(); }

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(string path) : base(path) { ReadSkeleton(); }

        /// <summary>
        /// All headers and segments of the file.
        /// </summary>
        readonly List<MatroskaTree> contents = new List<MatroskaTree>();

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            List<double> blockLengths = new List<double>();

            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                MatroskaTree latest = new MatroskaTree(reader);
                contents.Add(latest);
                if (latest.Tag == MatroskaTree.Segment) {
                    MatroskaTree segmentInfo = latest.GetChild(MatroskaTree.Segment_Info);
                    double length = segmentInfo.GetChild(MatroskaTree.Segment_Info_Duration).GetFloatBE(reader);
                    long scale = segmentInfo.GetChild(MatroskaTree.Segment_Info_TimestampScale).GetValue(reader);
                    blockLengths.Add(length * scale);
                }
            }

            Duration = QMath.Sum(blockLengths) * nsToS;
        }
    }
}