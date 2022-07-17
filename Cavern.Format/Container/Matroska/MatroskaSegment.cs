using System.Collections.Generic;
using System.IO;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// A <see cref="MatroskaTree"/> element with a seek header.
    /// </summary>
    internal class MatroskaSegment : MatroskaTree {
        /// <summary>
        /// The relative position of each tag in this segment.
        /// </summary>
        readonly Dictionary<int, long> seeks = new Dictionary<int, long>();

        /// <summary>
        /// Reads the segment with its seek header.
        /// </summary>
        public MatroskaSegment(Stream reader) : base(reader) {
            MatroskaTree seekHead = GetChild(reader, Segment_SeekHead);
            if (seekHead == null) {
                reader.Position = end;
                return;
            }

            MatroskaTree[] seekInputs = seekHead.GetChildren(reader, Segment_SeekHead_Seek);
            for (int i = 0; i < seekInputs.Length; ++i) {
                seeks[(int)seekInputs[i].GetChildValue(reader, Segment_SeekHead_Seek_SeekID)] =
                    seekInputs[i].GetChildValue(reader, Segment_SeekHead_Seek_SeekPosition);
            }
            reader.Position = end;
        }

        /// <summary>
        /// Tries to use the seek header to find the requested child. Searches through the segment if the child was not indexed.
        /// </summary>
        public MatroskaTree GetChildFromSeek(Stream reader, int childTag) {
            if (seeks.ContainsKey(childTag)) {
                reader.Position = position + seeks[childTag];
                return new MatroskaTree(reader);
            }
            return GetChild(reader, childTag);
        }
    }
}