using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.Matroska;
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

        readonly List<Cluster> clusters = new List<Cluster>();

        /// <summary>
        /// Matroska codec ID mapping to the <see cref="Codec"/> enum.
        /// </summary>
        static readonly Dictionary<string, Codec> codecNames = new Dictionary<string, Codec>() {
            ["V_MPEG4/ISO/AVC"] = Codec.AVC,
            ["V_MPEGH/ISO/HEVC"] = Codec.HEVC,
            ["A_DTS/LOSSLESS"] = Codec.DTS_HD,
            ["A_PCM/FLOAT/IEEE"] = Codec.PCM_Float,
            ["A_PCM/INT/LIT"] = Codec.PCM_LE,
            ["A_OPUS"] = Codec.Opus
        };

        /// <summary>
        /// Multiplier for all timestamps in <see cref="clusters"/>.
        /// </summary>
        long timestampScale;

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
                    timestampScale = segmentInfo.GetChild(MatroskaTree.Segment_Info_TimestampScale).GetValue(reader);
                    blockLengths.Add(length * timestampScale);

                    MatroskaTree tracklist = contents[i].GetChild(MatroskaTree.Segment_Tracks);
                    if (Tracks == null && tracklist != null)
                        ReadTracks(tracklist);

                    MatroskaTree[] blockClusters = contents[i].GetChildren(MatroskaTree.Segment_Cluster);
                    for (int cluster = 0; cluster < blockClusters.Length; ++cluster)
                        clusters.Add(new Cluster(reader, blockClusters[cluster]));
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
                MatroskaTree source = entries[track];
                entry.ID = source.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_TrackNumber);
                entry.Name = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_Name);
                entry.Language = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_Language);
                string codec = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_CodecID);
                if (codecNames.ContainsKey(codec))
                    entry.Format = codecNames[codec];
            }
        }
    }
}