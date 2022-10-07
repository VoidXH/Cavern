using System;
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
        /// Clusters are large and should be cached, but only to a certain limit to prevent leaking.
        /// The limit is the size of this array. Use <see cref="GetCluster(int)"/> to read a cluster,
        /// it checks the cache before trying to read it.
        /// </summary>
        readonly Tuple<int, Cluster>[] cachedClusters = new Tuple<int, Cluster>[] {
            new Tuple<int, Cluster>(-1, null), new Tuple<int, Cluster>(-1, null), new Tuple<int, Cluster>(-1, null),
            new Tuple<int, Cluster>(-1, null), new Tuple<int, Cluster>(-1, null) // Still better than null checks
        };

        /// <summary>
        /// All segments of the file.
        /// </summary>
        MatroskaSegment[] segments;

        /// <summary>
        /// Seek-aiding metadata.
        /// </summary>
        Cue[] cues;

        /// <summary>
        /// Multiplier for all timestamps in clusters.
        /// </summary>
        long timestampScale;

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(Stream reader) : base(reader) { ReadSkeleton(); }

        /// <summary>
        /// Minimal EBML reader.
        /// </summary>
        public MatroskaReader(string path) : base(path) { ReadSkeleton(); }

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the
        /// <see cref="ContainerReader.Tracks"/> array.</param>
        public override byte[] ReadNextBlock(int track) {
            MatroskaTrack trackData = Tracks[track] as MatroskaTrack;
            while (true) {
                Cluster data = GetCluster(trackData.lastCluster);
                if (data == null) {
                    return null;
                }
                IReadOnlyList<Block> blocks = data.GetBlocks(reader);
                int count = blocks.Count;
                while (trackData.lastBlock < count) {
                    Block block = blocks[trackData.lastBlock++];
                    if (block.Track == trackData.ID) {
                        return block.GetData();
                    }
                }
                trackData.lastBlock = 0;
                ++trackData.lastCluster;
            }
        }

        /// <summary>
        /// Start the following reads from the selected timestamp.
        /// Seeks all tracks to the block before the position given in seconds.
        /// </summary>
        /// <returns>Position that was actually possible to seek to or -1 if the position didn't change.</returns>
        public override double Seek(double position) {
            long targetTime = (long)(position * sToNs / timestampScale);
            Cue cue = Cue.Find(cues, targetTime);
            int clusterId = 0;
            Cluster cluster;

            if (cue != null) {
                clusterId = segments[0].GetIndexByPosition(reader, MatroskaTree.Segment_Cluster, cue.Position);
            }
            if (clusterId == -1) {
                while (true) {
                    cluster = GetCluster(clusterId++);
                    if (cluster == null) {
                        break;
                    }
                    if (cluster.TimeStamp > targetTime) {
                        break;
                    }
                }
                if (cluster == null) {
                    return -1;
                }
                --clusterId;
            }

            bool[] trackSet = new bool[Tracks.Length];
            int tracksSet = 0;
            long audioTime = long.MaxValue, minTime = long.MaxValue;
            while (clusterId >= 0 && tracksSet != trackSet.Length) {
                cluster = GetCluster(clusterId);
                IReadOnlyList<Block> blocks = cluster.GetBlocks(reader);
                for (int block = blocks.Count - 1; block >= 0; --block) {
                    long trackId = Tracks.GetIndexByID(blocks[block].Track);
                    if (trackId == -1 || trackSet[trackId]) {
                        continue;
                    }
                    long time = cluster.TimeStamp + blocks[block].TimeStamp;
                    if (time <= targetTime) {
                        MatroskaTrack trackData = Tracks[trackId] as MatroskaTrack;
                        trackData.lastCluster = clusterId;
                        trackData.lastBlock = block;
                        if (minTime > time) {
                            minTime = time;
                        }
                        if (trackData.Format.IsAudio() && audioTime > targetTime) {
                            audioTime = time;
                        }
                        trackSet[trackId] = true;
                        ++tracksSet;
                    }
                }
                --clusterId;
            }

            if (audioTime != long.MaxValue) {
                return audioTime * timestampScale * nsToS;
            }
            if (minTime != long.MaxValue) {
                return minTime * timestampScale * nsToS;
            }
            return -1;
        }

        /// <summary>
        /// Read a <see cref="Cluster"/> from the cache, or if it's not cached, from the file.
        /// </summary>
        Cluster GetCluster(int index) {
            for (int i = 0; i < cachedClusters.Length; ++i) {
                if (cachedClusters[i].Item1 == index) {
                    return cachedClusters[i].Item2;
                }
            }

            for (int i = 0; i < segments.Length; ++i) {
                MatroskaTree cluster = segments[i].GetChild(reader, MatroskaTree.Segment_Cluster, index);
                if (cluster != null) {
                    Array.Copy(cachedClusters, 1, cachedClusters, 0, cachedClusters.Length - 1);
                    Cluster newCluster = new Cluster(reader, cluster);
                    cachedClusters[^1] = new Tuple<int, Cluster>(index, newCluster);
                    return newCluster;
                }
            }
            return null;
        }

        /// <summary>
        /// Read the metadata and basic block structure of the file.
        /// </summary>
        void ReadSkeleton() {
            List<MatroskaSegment> segmentSource = new List<MatroskaSegment>();
            new MatroskaTree(reader); // Skip EBML header
            while (reader.Position < reader.Length) {
                MatroskaSegment segment = new MatroskaSegment(reader);
                if (segment.Tag != MatroskaTree.Segment) {
                    continue;
                }
                segmentSource.Add(segment);
            }
            segments = segmentSource.ToArray();

            List<double> blockLengths = new List<double>();
            for (int i = 0; i < segments.Length; ++i) {
                MatroskaTree segmentInfo = segments[i].GetChild(reader, MatroskaTree.Segment_Info);
                double length = segmentInfo.GetChild(reader, MatroskaTree.Segment_Info_Duration).GetFloatBE(reader);
                timestampScale = segmentInfo.GetChildValue(reader, MatroskaTree.Segment_Info_TimestampScale);
                blockLengths.Add(length * timestampScale);

                cues = Cue.GetCues(segments[i], reader);
                MatroskaTree tracklist = segments[i].GetChild(reader, MatroskaTree.Segment_Tracks);
                if (Tracks == null && tracklist != null) {
                    ReadTracks(tracklist);
                }
            }
            Duration = QMath.Sum(blockLengths) * nsToS;
        }

        /// <summary>
        /// Read track information metadata.
        /// </summary>
        void ReadTracks(MatroskaTree tracklist) {
            MatroskaTree[] entries = tracklist.GetChildren(reader, MatroskaTree.Segment_Tracks_TrackEntry);
            Tracks = new Track[entries.Length];
            for (int track = 0; track < entries.Length; ++track) {
                MatroskaTrack entry = new MatroskaTrack(this, track);
                Tracks[track] = entry;

                MatroskaTree source = entries[track];
                entry.ID = source.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_TrackNumber);
                entry.Name = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_Name);
                entry.Language = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_Language);
                if (string.IsNullOrEmpty(entry.Language)) {
                    entry.Language = defaultLanguage;
                }
                string codec = source.GetChildUTF8(reader, MatroskaTree.Segment_Tracks_TrackEntry_CodecID);
                if (codecNames.ContainsKey(codec)) {
                    entry.Format = codecNames[codec];
                }

                MatroskaTree audioData = source.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_Audio);
                if (audioData != null) {
                    entry.Extra = new TrackExtraAudio {
                        SampleRate = audioData.GetChildFloatBE(reader,
                            MatroskaTree.Segment_Tracks_TrackEntry_Audio_SamplingFrequency),
                        ChannelCount = (int)audioData.GetChildValue(reader,
                            MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels),
                        Bits = (BitDepth)audioData.GetChildValue(reader,
                            MatroskaTree.Segment_Tracks_TrackEntry_Audio_BitDepth)
                    };
                }
            }
        }

        /// <summary>
        /// Nanoseconds to seconds.
        /// </summary>
        const double nsToS = 1.0 / 1000000000;

        /// <summary>
        /// Seconds to nanoseconds.
        /// </summary>
        const double sToNs = 1000000000;

        /// <summary>
        /// A Matroska track's default language.
        /// </summary>
        const string defaultLanguage = "eng";

        /// <summary>
        /// Matroska codec ID mapping to the <see cref="Codec"/> enum.
        /// </summary>
        readonly static Dictionary<string, Codec> codecNames = new Dictionary<string, Codec>() {
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