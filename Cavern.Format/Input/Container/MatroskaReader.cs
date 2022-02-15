using System.Collections.Generic;
using System.IO;

namespace Cavern.Format.Container {
    /// <summary>
    /// Reads EBML, a kind of binary XML format that is used by Matroska.
    /// </summary>
    public class MatroskaReader : ContainerReader {
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
        readonly List<KeyLengthValueTree> contents = new List<KeyLengthValueTree>();

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
                contents.Add(new KeyLengthValueTree(reader));
        }
    }
}