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
        /// Last byte (exclusive) of the file that is a tag in this element.
        /// </summary>
        protected readonly long end;

        /// <summary>
        /// The contained subtree.
        /// </summary>
        readonly List<MatroskaTree> children = new List<MatroskaTree>();

        /// <summary>
        /// Location in the file where the next child should be read from.
        /// </summary>
        long nextTag;

        /// <summary>
        /// Cache for <see cref="GetChild(Stream, int, int)"/>, contains which the indices of
        /// already read <see cref="children"/> are for a given tag (key).
        /// </summary>
        Dictionary<int, List<int>> childIndices;

        /// <summary>
        /// Build the next KLV subtree.
        /// </summary>
        public MatroskaTree(Stream reader) : base(reader) {
            nextTag = reader.Position;
            end = nextTag + Length;
            reader.Position = end;
        }

        /// <summary>
        /// Build the next KLV subtree while checking if it's in range of the file (<paramref name="valid"/>) or not.
        /// </summary>
        MatroskaTree(Stream reader, long endPosition, out bool valid) : base(reader) {
            nextTag = reader.Position;
            end = nextTag + Length;
            valid = end < endPosition;
            if (valid) {
                reader.Position = end;
            }
        }

        /// <summary>
        /// Parses a tree item if possible, returns null if not.
        /// </summary>
        /// <param name="reader">Matroska stream to read from</param>
        /// <param name="endPosition">Location of the final byte in the stream (exclusive).</param>
        public static MatroskaTree TryCreate(Stream reader, long endPosition) {
            MatroskaTree result = new MatroskaTree(reader, endPosition, out bool valid);
            return valid ? result : null;
        }

        /// <summary>
        /// Fetch the first child of a tag if it exists.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag) {
            for (int i = 0, c = children.Count; i < c; ++i) {
                if (children[i].Tag == tag) {
                    return children[i];
                }
            }

            reader.Position = nextTag;
            while (nextTag < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag) {
                    return subtree;
                }
                nextTag = reader.Position;
            }
            return null;
        }

        /// <summary>
        /// Get a specific child by its order of the same kind of children.
        /// </summary>
        public MatroskaTree GetChild(Stream reader, int tag, int index) {
            childIndices ??= new Dictionary<int, List<int>>();
            List<int> indices;
            if (childIndices.ContainsKey(tag)) {
                indices = childIndices[tag];
            } else {
                indices = childIndices[tag] = new List<int>();
            }

            int c = indices.Count;
            if (index < indices.Count) {
                return children[indices[index]];
            }

            int lastChild = 0;
            if (c != 0) {
                lastChild = indices[c - 1] + 1;
            }
            for (int i = lastChild, childCount = children.Count; i < childCount; ++i) {
                if (children[i].Tag == tag) {
                    indices.Add(i);
                    if (c++ == index) {
                        return children[i];
                    }
                }
            }

            int tagIndex = children.Count;
            reader.Position = nextTag;
            while (nextTag < end) {
                MatroskaTree subtree = TryCreate(reader, end);
                if (subtree == null) {
                    nextTag = end;
                    return null;
                }

                children.Add(subtree);
                if (subtree.Tag == tag) {
                    indices.Add(tagIndex);
                    if (c++ == index) {
                        nextTag = reader.Position;
                        return subtree;
                    }
                }
                ++tagIndex;
            }
            nextTag = reader.Position;
            return null;
        }

        /// <summary>
        /// Fetch all child instances of a tag.
        /// </summary>
        public MatroskaTree[] GetChildren(Stream reader, int tag) {
            int tags = 0;
            for (int i = 0, c = children.Count; i < c; ++i) {
                if (children[i].Tag == tag) {
                    ++tags;
                }
            }

            reader.Position = nextTag;
            while (reader.Position < end) {
                MatroskaTree subtree = new MatroskaTree(reader);
                children.Add(subtree);
                if (subtree.Tag == tag) {
                    ++tags;
                }
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
        /// Get the first found child's big-endian floating point value by tag if it exists.
        /// </summary>
        public double GetChildFloatBE(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetFloatBE(reader) : -1;
        }

        /// <summary>
        /// Get the first found child's UTF-8 value by tag if it exists.
        /// </summary>
        public string GetChildUTF8(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetUTF8(reader) : string.Empty;
        }

        /// <summary>
        /// Get the first found child's <see cref="VarInt"/> value by tag if it exists.
        /// </summary>
        public long GetChildValue(Stream reader, int tag) {
            MatroskaTree child = GetChild(reader, tag);
            return child != null ? child.GetValue(reader) : -1;
        }

        /// <summary>
        /// Get the index of a child for <see cref="GetChild(Stream, int, int)"/> by its position in the file stream.
        /// </summary>
        /// <remarks>This <paramref name="position"/> is not the same as <see cref="KeyLengthValue.position"/>, that
        /// has to be matched first by reading the metadata for the element.</remarks>
        public int GetIndexByPosition(Stream reader, int tag, long position) {
            reader.Position = position;
            MatroskaTree element = new MatroskaTree(reader);
            position = element.position;

            int result = 0;
            while (true) {
                MatroskaTree child = GetChild(reader, tag, result);
                if (child == null) {
                    return -1;
                }
                if (child.position == position) {
                    return result;
                }
                ++result;
            }
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

        /// <summary>
        /// Display the tag in HEX when converting to string.
        /// </summary>
        public override string ToString() => "0x" + Tag.ToString("X8");

        /// <summary>
        /// EBML tag IDs.
        /// </summary>
        internal const int Segment_Tracks_TrackEntry_TrackType = 0x83,
            Segment_Tracks_TrackEntry_CodecID = 0x86,
            Segment_Tracks_TrackEntry_FlagLacing = 0x9C,
            Segment_Tracks_TrackEntry_Audio_Channels = 0x9F,
            Segment_Cluster_BlockGroup = 0xA0,
            Segment_Cluster_BlockGroup_Block = 0xA1,
            Segment_Cluster_SimpleBlock = 0xA3,
            Segment_Tracks_TrackEntry = 0xAE,
            Segment_Tracks_TrackEntry_Video_PixelWidth = 0xB0,
            Segment_Cues_CuePoint_CueTime = 0xB3,
            Segment_Tracks_TrackEntry_Audio_SamplingFrequency = 0xB5,
            Segment_Cues_CuePoint_CueTrackPositions = 0xB7,
            Segment_Tracks_TrackEntry_Video_PixelHeight = 0xBA,
            Segment_Cues_CuePoint = 0xBB,
            Segment_Tracks_TrackEntry_TrackNumber = 0xD7,
            Segment_Tracks_TrackEntry_Video = 0xE0,
            Segment_Tracks_TrackEntry_Audio = 0xE1,
            Segment_Cluster_Timestamp = 0xE7,
            Segment_Cues_CuePoint_CueTrackPositions_CueClusterPosition = 0xF1,
            Segment_Cues_CuePoint_CueTrackPositions_CueTrack = 0xF7,
            Segment_Tracks_TrackEntry_BlockAdditionMapping = 0x41E4,
            Segment_Info_Duration = 0x4489,
            Segment_Info_MuxingApp = 0x4D80,
            Segment_SeekHead_Seek = 0x4DBB,
            Segment_Tracks_TrackEntry_Name = 0x536E,
            Segment_SeekHead_Seek_SeekID = 0x53AB,
            Segment_SeekHead_Seek_SeekPosition = 0x53AC,
            Segment_Tracks_TrackEntry_Video_Colour = 0x55B0,
            Segment_Tracks_TrackEntry_Video_Colour_Range = 0x55B9,
            Segment_Info_WritingApp = 0x5741,
            Segment_Tracks_TrackEntry_Audio_BitDepth = 0x6264,
            Segment_Info_ChapterTranslate = 0x6924,
            Segment_Tracks_TrackEntry_CodecPrivate = 0x63A2,
            Segment_Tracks_TrackEntry_TrackUID = 0x73C5,
            Segment_Tracks_TrackEntry_Language = 0x22B59C,
            Segment_Tracks_TrackEntry_DefaultDuration = 0x23E383,
            Segment_Info_TimestampScale = 0x2AD7B1,
            Segment_SeekHead = 0x114D9B74,
            Segment_Info = 0x1549A966,
            Segment_Tracks = 0x1654AE6B,
            Segment = 0x18538067,
            EBML = 0x1A45DFA3,
            EBML_Void = 0xEC,
            EBML_LE = unchecked((int)0xA3DF451A),
            EBML_Version = 0x4286,
            EBML_ReadVersion = 0x42F7,
            EBML_MaxIDLength = 0x42F2,
            EBML_MaxSizeLength = 0x42F3,
            EBML_DocType = 0x4282,
            EBML_DocTypeVersion = 0x4287,
            EBML_DocTypeReadVersion = 0x4285,
            Segment_Cues = 0x1C53BB6B,
            Segment_Cluster = 0x1F43B675;

        /// <summary>
        /// Matroska codec ID mapping to the <see cref="Codec"/> enum.
        /// </summary>
        public static readonly Dictionary<string, Codec> codecNames = new Dictionary<string, Codec> {
            ["V_MPEG4/ISO/AVC"] = Codec.AVC,
            ["V_MPEGH/ISO/HEVC"] = Codec.HEVC,
            ["A_AC3"] = Codec.AC3,
            ["A_DTS"] = Codec.DTS,
            ["A_DTS/LOSSLESS"] = Codec.DTS_HD,
            ["A_EAC3"] = Codec.EnhancedAC3,
            ["A_FLAC"] = Codec.FLAC,
            ["A_OPUS"] = Codec.Opus,
            ["A_PCM/FLOAT/IEEE"] = Codec.PCM_Float,
            ["A_PCM/INT/LIT"] = Codec.PCM_LE,
            ["A_TRUEHD"] = Codec.TrueHD,
        };
    }
}