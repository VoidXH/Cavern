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
        /// <inheritdoc/>
        public override Common.Container Type => Common.Container.Matroska;

        /// <summary>
        /// Clusters are large and should be cached, but only to a certain limit to prevent leaking.
        /// The limit is the size of this array. Use <see cref="GetCluster(int)"/> to read a cluster,
        /// it checks the cache before trying to read it.
        /// </summary>
        readonly Tuple<int, Cluster>[] cachedClusters = {
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
        /// The following block of the track is rendered and available.
        /// </summary>
        public override bool IsNextBlockAvailable(int track) => GetBlock(track) != null;

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the
        /// <see cref="ContainerReader.Tracks"/> array.</param>
        public override byte[] ReadNextBlock(int track) {
            Block nextBlock = GetBlock(track);
            ((MatroskaTrack)Tracks[track]).lastBlock++;
            return nextBlock?.GetData();
        }

        /// <summary>
        /// Returns if the next block of a track can be completely decoded by itself.
        /// </summary>
        public override bool IsNextBlockKeyframe(int track) {
            Block nextBlock = GetBlock(track);
            return nextBlock != null && nextBlock.IsKeyframe;
        }

        /// <summary>
        /// Get what is the time offset of the next block in seconds.
        /// </summary>
        /// <returns>Time offset in seconds, or -1 if the last block was passed.</returns>
        public override double GetNextBlockOffset(int track) {
            Block nextBlock = GetBlock(track);
            if (nextBlock != null) {
                Cluster cluster = GetCluster(((MatroskaTrack)Tracks[track]).lastCluster);
                return (cluster.TimeStamp + nextBlock.TimeStamp) * timestampScale * nsToS;
            }
            return -1;
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
        /// Get the block in line for a <see cref="Track"/>. Does not increment the block counter, just gets the <see cref="Block"/>.
        /// To read the next block, increment <see cref="MatroskaTrack.lastBlock"/>, then call this function.
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        Block GetBlock(int track) {
            MatroskaTrack trackData = Tracks[track] as MatroskaTrack;
            while (true) {
                Cluster data = GetCluster(trackData.lastCluster);
                if (data == null) {
                    return null;
                }
                IReadOnlyList<Block> blocks = data.GetBlocks(reader);
                int count = blocks.Count;
                while (trackData.lastBlock < count) {
                    Block block = blocks[trackData.lastBlock];
                    if (block.Track == trackData.ID) {
                        return block;
                    }
                    trackData.lastBlock++;
                }
                trackData.lastBlock = 0;
                trackData.lastCluster++;
            }
        }

        /// <summary>
        /// Read a <see cref="Cluster"/> from the cache, or if it's not cached, from the file.
        /// </summary>
        Cluster GetCluster(int index) {
            for (int i = 0; i < cachedClusters.Length; i++) {
                if (cachedClusters[i].Item1 == index) {
                    return cachedClusters[i].Item2;
                }
            }

            for (int i = 0; i < segments.Length; i++) {
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
            for (int i = 0; i < segments.Length; i++) {
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
            Duration = blockLengths.Sum() * nsToS;
        }

        /// <summary>
        /// Read track information metadata.
        /// </summary>
        void ReadTracks(MatroskaTree tracklist) {
            MatroskaTree[] entries = tracklist.GetChildren(reader, MatroskaTree.Segment_Tracks_TrackEntry);
            Tracks = new Track[entries.Length];
            for (int track = 0; track < entries.Length; track++) {
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
                if (MatroskaTree.codecNames.ContainsKey(codec)) {
                    entry.Format = MatroskaTree.codecNames[codec];
                }

                MatroskaTree data = source.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_Video);
                if (data != null) {
                    MatroskaTree codecPrivate = source.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_CodecPrivate);
                    MatroskaTrackExtraVideo extra = new MatroskaTrackExtraVideo(reader, data);
                    entry.Extra = extra;
                    extra.PrivateData = codecPrivate?.GetBytes(reader);

                    long defaultDuration = source.GetChildValue(reader, MatroskaTree.Segment_Tracks_TrackEntry_DefaultDuration);
                    if (defaultDuration != -1) {
                        extra.FrameRate = sToNs / defaultDuration;
                    }
                    MatroskaTree addition = source.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_BlockAdditionMapping);
                    if (addition != null) {
                        extra.BlockAdditionMapping = addition.GetBytes(reader);
                    }
                } else {
                    data = source.GetChild(reader, MatroskaTree.Segment_Tracks_TrackEntry_Audio);
                    if (data != null) {
                        entry.Extra = new TrackExtraAudio(reader, data);
                    }
                }
            }
        }

        /// <summary>
        /// Nanoseconds to seconds.
        /// </summary>
        internal const double nsToS = 1.0 / 1000000000;

        /// <summary>
        /// Seconds to nanoseconds.
        /// </summary>
        internal const double sToNs = 1000000000;

        /// <summary>
        /// A Matroska track's default language.
        /// </summary>
        const string defaultLanguage = "eng";
    }
}