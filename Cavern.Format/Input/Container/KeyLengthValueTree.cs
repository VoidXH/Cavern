using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Container {
    /// <summary>
    /// Builds a tree of the EBML's needed tags.
    /// </summary>
    /// <see href="https://github.com/ietf-wg-cellar/matroska-specification/blob/master/ebml_matroska.xml"/>
    internal class KeyLengthValueTree : KeyLengthValue {
        /// <summary>
        /// EBML tag IDs.
        /// </summary>
        const int Segment_Cluster_BlockGroup = 0xA0,
            Segment_Tracks_TrackEntry = 0xAE,
            Segment_SeekHead_Seek = 0x4DBB,
            Segment_Info_ChapterTranslate = 0x6924,
            Segment_SeekHead = 0x114D9B74,
            Segment_Info = 0x1549A966,
            Segment_Tracks = 0x1654AE6B,
            Segment = 0x18538067,
            EBML = 0x1A45DFA3,
            Segment_Cues = 0x1C53BB6B,
            Segment_Cluster = 0x1F43B675;

        /// <summary>
        /// Tags which have metadata in their children.
        /// </summary>
        /// <remarks>They have to be in ascending order for the binary search to work.</remarks>
        internal readonly static int[] hasChildren = new int[] {
            Segment_Cluster_BlockGroup, Segment_Tracks_TrackEntry, Segment_SeekHead_Seek, Segment_Info_ChapterTranslate,
            Segment_SeekHead, Segment_Info, Segment_Tracks, Segment, EBML, Segment_Cues, Segment_Cluster
        };

        /// <summary>
        /// The contained subtree. Key is the tag ID.
        /// </summary>
        public List<KeyLengthValueTree> Children { get; private set; }

        /// <summary>
        /// Build the next KLV subtree.
        /// </summary>
        public KeyLengthValueTree(BinaryReader reader) : base(reader) {
            if (Array.BinarySearch(hasChildren, Tag) < 0) {
                Skip(reader);
                return;
            }

            Children = new List<KeyLengthValueTree>();
            long end = reader.BaseStream.Position + Length;
            while (reader.BaseStream.Position < end) {
                KeyLengthValueTree subtree = new KeyLengthValueTree(reader);
                Children.Add(subtree);
            }
        }
    }
}