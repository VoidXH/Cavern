using System.Collections.Generic;
using System.IO;
using Cavern.Format.Common;
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
        /// Metadata of media streams in this file.
        /// </summary>
        public Track[] Tracks { get; private set; }

        /// <summary>
        /// All headers and segments of the file.
        /// </summary>
        readonly List<MatroskaTree> contents = new List<MatroskaTree>();

        /// <summary>
        /// Matroska codec ID mapping to the <see cref="Codec"/> enum.
        /// </summary>
        static readonly Dictionary<string, Codec> codecNames = new Dictionary<string, Codec>() {
            ["V_MPEGH/ISO/HEVC"] = Codec.HEVC,
            ["A_OPUS"] = Codec.Opus
        };

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(BinaryReader reader) : base(reader) { ReadSkeleton(); }

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(string path) : base(path) { ReadSkeleton(); }

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
                contents.Add(new MatroskaTree(reader));

            List<double> blockLengths = new List<double>();
            for (int i = 0, c = contents.Count; i < c; ++i) {
                if (contents[i].Tag == MatroskaTree.Segment) {
                    MatroskaTree segmentInfo = contents[i].GetChild(MatroskaTree.Segment_Info);
                    double length = segmentInfo.GetChild(MatroskaTree.Segment_Info_Duration).GetFloatBE(reader);
                    long scale = segmentInfo.GetChild(MatroskaTree.Segment_Info_TimestampScale).GetValue(reader);
                    blockLengths.Add(length * scale);

                    MatroskaTree tracklist = contents[i].GetChild(MatroskaTree.Segment_Tracks);
                    if (Tracks == null && tracklist != null)
                        ReadTracks(tracklist);
                }
            }
            Duration = QMath.Sum(blockLengths) * nsToS;
        }

        /// <summary>
        /// Read track information metadata.
        /// </summary>
        void ReadTracks(MatroskaTree tracklist) {
            MatroskaTree[] entries = tracklist.GetChildren(MatroskaTree.Segment_Tracks_TrackEntry);
            Tracks = new Track[entries.Length];
            for (int track = 0; track < entries.Length; ++track) {
                Track entry = Tracks[track] = new Track();
                MatroskaTree source = entries[track].GetChild(MatroskaTree.Segment_Tracks_TrackEntry_Name);
                if (source != null)
                    entry.Name = source.GetUTF8(reader);
                source = entries[track].GetChild(MatroskaTree.Segment_Tracks_TrackEntry_Language);
                if (source != null)
                    entry.Language = source.GetUTF8(reader);
                source = entries[track].GetChild(MatroskaTree.Segment_Tracks_TrackEntry_CodecID);
                if (source != null) {
                    string codec = source.GetUTF8(reader);
                    if (codecNames.ContainsKey(codec))
                        entry.Format = codecNames[codec];
                }
            }
        }
    }
}