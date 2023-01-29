using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// A box that contains other boxes.
    /// </summary>
    internal class NestedBox : Box {
        /// <summary>
        /// All child boxes contained in this box.
        /// </summary>
        public Box[] Contents { get; }

        /// <summary>
        /// Get the first child of the selected header.
        /// </summary>
        public Box this[uint header] {
            get {
                for (int i = 0; i < Contents.Length; i++) {
                    if (Contents[i].Header == header) {
                        return Contents[i];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Parse the nested boxes.
        /// </summary>
        public NestedBox(uint length, uint header, Stream reader) : base(length, header, reader) {
            List<Box> contents = new List<Box>();
            long end = reader.Position + length;
            while (reader.Position < end) {
                contents.Add(Parse(reader));
            }
            Contents = contents.ToArray();
        }

        /// <summary>
        /// List the nested boxes in a string.
        /// </summary>
        public override string ToString() => $"Nested into {Header.ToFourCC()}: " +
            string.Join(", ", Contents.Select(x => x.Header.ToFourCC()));
    }
}