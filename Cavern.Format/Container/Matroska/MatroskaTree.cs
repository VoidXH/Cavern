using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Builds a tree of a Matroska file's neccessary tags.
    /// </summary>
    /// <see href="https://github.com/ietf-wg-cellar/matroska-specification/blob/master/ebml_matroska.xml"/>
    internal class MatroskaTree : KeyLengthValue {
        /// <summary>
        /// EBML tag IDs.
        /// </summary>
        internal const int Segment_Tracks_TrackEntry_CodecID = 0x86,
            Segment_Tracks_TrackEntry_Audio_Channels = 0x9F,
            Segment_Cluster_BlockGroup = 0xA0,
            Segment_Cluster_BlockGroup_Block = 0xA1,
            Segment_Cluster_SimpleBlock = 0xA3,
            Segment_Tracks_TrackEntry = 0xAE,
            Segment_Tracks_TrackEntry_Audio_SamplingFrequency = 0xB5,
            Segment_Tracks_TrackEntry_TrackNumber = 0xD7,
            Segment_Tracks_TrackEntry_Audio = 0xE1,
            Segment_Cluster_Timestamp = 0xE7,
            Segment_Info_Duration = 0x4489,
            Segment_SeekHead_Seek = 0x4DBB,
            Segment_Tracks_TrackEntry_Name = 0x536E,
            Segment_Tracks_TrackEntry_Audio_BitDepth = 0x6264,
            Segment_Info_ChapterTranslate = 0x6924,
            Segment_Tracks_TrackEntry_Language = 0x22B59C,
            Segment_Info_TimestampScale = 0x2AD7B1,
            Segment_SeekHead = 0x114D9B74,
            Segment_Info = 0x1549A966,
            Segment_Tracks = 0x1654AE6B,
            Segment = 0x18538067,
            EBML = 0x1A45DFA3,
            Segment_Cues = 0x1C53BB6B,
            Segment_Cluster = 0x1F43B675;

        /// <summary>
        /// Tags which have metadata in their children. They are parsed when the MKV file is read.
        /// </summary>
        /// <remarks>They have to be in ascending order for the binary search to work.</remarks>
        internal static readonly int[] hasChildren = new int[] {
            Segment_Cluster_BlockGroup, Segment_Tracks_TrackEntry, Segment_Tracks_TrackEntry_Audio, Segment_SeekHead_Seek,
            Segment_Info_ChapterTranslate, Segment_SeekHead, Segment_Info, Segment_Tracks, Segment, EBML, Segment_Cues,
            Segment_Cluster
        };

        /// <summary>
        /// The contained subtree.
        /// </summary>
        readonly List<MatroskaTree> children = new List<MatroskaTree>();

        /// <summary>
        /// Last byte (exclusive) of the file that is a tag in this element.
        /// </summary>
        readonly long end;

        /// <summary>
        /// Cache for <see cref="GetChild(BinaryReader, int, int)"/>, contains which the indices of
        /// already read <see cref="children"/> are for a given tag (key).
        /// </summary>
        Dictionary<int, List<int>> childIndices;

        /// <summary>
        /// Location in the file where the next child should be read from.
        /// </summary>
        long nextTag;

        /// <summary>
        /// Build the next KLV subtree.
        /// </summary>
        public MatroskaTree(Stream reader) : base(reader) {
            nextTag = reader.Position;
            end = nextTag + Length;
            reader.Position = end;
        }

        /// <summary>
        /// Fetch the first child of a tag if it exists.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag) {
            for (int i = 0, c = children.Count; i < c; ++i)
                if (children[i].Tag == tag)
                    return children[i];
            reader.Position = nextTag;

            while (nextTag < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag)
                    return subtree;
                nextTag = reader.Position;
            }
            return null;
        }

        /// <summary>
        /// Get a specific child by its order of the same kind of children.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag, int index) {
            if (childIndices == null)
                childIndices = new Dictionary<int, List<int>>();
            List<int> indices;
            if (childIndices.ContainsKey(tag))
                indices = childIndices[tag];
            else
                indices = childIndices[tag] = new List<int>();

            int c = indices.Count;
            if (index < indices.Count)
                return children[indices[index]];

            int lastChild = 0;
            if (c != 0)
                lastChild = indices[c - 1] + 1;
            for (int i = lastChild; i < c; ++i) {
                if (children[i].Tag == tag) {
                    indices.Add(i);
                    if (c++ == index)
                        return children[i];
                }
            }

            while (nextTag < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag) {
                    indices.Add(c);
                    if (c++ == index)
                        return subtree;
                }
                nextTag = reader.Position;
            }
            return null;
        }

        /// <summary>
        /// Fetch all child instances of a tag.
        /// </summary>
        public MatroskaTree[] GetChildren(Stream reader, int tag) {
            int tags = 0;
            for (int i = 0, c = children.Count; i < c; ++i)
                if (children[i].Tag == tag)
                    ++tags;
            reader.Position = nextTag;
            while (reader.Position < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag)
                    ++tags;
            }
            nextTag = end;

            MatroskaTree[] result = new MatroskaTree[tags];
            for (int i = 0, c = children.Count; i < c; ++i) {
                if (children[i].Tag == tag) {
                    result[^tags] = children[i];
                    --tags;
                }
            }
            return result;
        }

        /// <summary>
        /// Fetch the first child by a tag path if it exists.
        /// </summary>
        public MatroskaTree GetChildByPath(Stream reader, params int[] path) {
            MatroskaTree result = this;
            for (int depth = 0; depth < path.Length; ++depth) {
                result = result.GetChild(reader, path[depth]);
                if (result == null)
                    return null;
            }
            return result;
        }

        /// <summary>
        /// Fetch all child instances which have a given path.
        /// </summary>
        public List<MatroskaTree> GetChildrenByPath(Stream reader, params int[] path) {
            List<MatroskaTree> check = new List<MatroskaTree>() { this },
                queue = new List<MatroskaTree>();
            for (int depth = 0; depth < path.Length; ++depth) {
                queue.Clear();
                for (int i = 0, c = check.Count; i < c; ++i)
                    queue.AddRange(check[i].GetChildren(reader, path[depth]));
                (check, queue) = (queue, check);
            }
            return check;
        }

        /// <summary>
        /// Get the first found child's big-endian floating point value by tag if it exists.
        /// </summary>
        public double GetChildFloatBE(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            if (child != null)
                return child.GetFloatBE(reader);
            return -1;
        }

        /// <summary>
        /// Get the first found child's UTF-8 value by tag if it exists.
        /// </summary>
        public string GetChildUTF8(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            if (child != null)
                return child.GetUTF8(reader);
            return string.Empty;
        }

        /// <summary>
        /// Get the first found child's <see cref="VarInt"/> value by tag if it exists.
        /// </summary>
        public long GetChildValue(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            if (child != null)
                return child.GetValue(reader);
            return -1;
        }

        /// <summary>
        /// Read the remainder of the value of this element to a byte array.
        /// </summary>
        public byte[] GetRawData(Stream reader) {
            reader.Position = nextTag;
            byte[] result = new byte[end - nextTag];
            reader.Read(result);
            return result;
        }
    }
}