using System.Collections.Generic;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Container.Matroska;
using Cavern.Utilities;

namespace Cavern.Format.Container {
    /// <summary>
    /// Writes source <see cref="Track"/>s to a Matroska file.
    /// </summary>
    public class MatroskaWriter : ContainerWriter {
        /// <summary>
        /// Writes the structure of the EBML file.
        /// </summary>
        MatroskaTreeWriter tree;

        /// <summary>
        /// Writing a <see cref="Cluster"/> is in progress and it's not closed.
        /// </summary>
        bool inCluster;

        /// <summary>
        /// Seconds of content already written to the file.
        /// </summary>
        double position;

        /// <summary>
        /// The <see cref="position"/> where the last cluster was opened. Used for relative <see cref="Block"/> timestamps.
        /// </summary>
        double lastClusterStarted;

        /// <summary>
        /// The index of the track that contains the main video. Used to create the <see cref="cues"/>.
        /// </summary>
        int mainTrack;

        /// <summary>
        /// The start position of the main segment in the output stream. Cues are written relative to this.
        /// This is also the position where the <see cref="seekHead"/> will be written.
        /// </summary>
        long segmentOffset;

        /// <summary>
        /// The start position of the currently written cluster in the output stream, used for positioning <see cref="cues"/>.
        /// </summary>
        long clusterOffset;

        /// <summary>
        /// List of all keyframes that will be written to the <see cref="MatroskaTree.Segment_Cues"/> element.
        /// This only includes video frames (main track), as audio can be found in the same cluster, always being keyframes.
        /// One cue will be created for each cluster, the first keyframe in them.
        /// </summary>
        readonly List<Cue> cues = new List<Cue>();

        /// <summary>
        /// Positions of major elements inside the main <see cref="MatroskaTree.Segment"/>.
        /// </summary>
        readonly List<(int tag, long offset)> seekHead = new List<(int tag, long offset)>();

        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(Stream writer, Track[] tracks, double duration) : base(writer, tracks, duration) => SetMainTrack();

        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(string path, Track[] tracks, double duration) : base(path, tracks, duration) => SetMainTrack();

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public override void WriteHeader() {
            tree = new MatroskaTreeWriter(writer);
            CreateEBMLHeader();
            tree.OpenSequence(MatroskaTree.Segment, 6);
            segmentOffset = writer.Position;

            // Create a placeholder for the seek header.
            tree.OpenSequence(MatroskaTree.EBML_Void, voidOverhead - 1);
            writer.Write(new byte[maxSeekHeadSize - voidOverhead]);
            tree.CloseSequence();

            CreateSegmentInfo();
            CreateSegmentTracks();
        }

        /// <summary>
        /// Write the frames that are part of the next block with of a given <paramref name="blockDuration"/>.
        /// </summary>
        /// <returns>The writing has finished.</returns>
        public override bool WriteBlock(double blockDuration) {
            if (!inCluster) {
                inCluster = true;
                lastClusterStarted = position;
                clusterOffset = writer.Position;
                tree.OpenSequence(MatroskaTree.Segment_Cluster, 4);
                uint pos = (uint)(position * MatroskaReader.sToNs / timestampScale);
                if (pos < short.MaxValue) {
                    tree.Write(MatroskaTree.Segment_Cluster_Timestamp, (short)pos);
                } else {
                    tree.Write(MatroskaTree.Segment_Cluster_Timestamp, pos);
                }
            }

            double timeInCluster = position - lastClusterStarted;
            uint relativeTimestamp = (uint)(timeInCluster * MatroskaReader.sToNs / timestampScale);
            // Start a new cluster when the available bits for offset are exhausted or the cluster is too long
            if (relativeTimestamp > short.MaxValue || timeInCluster > maxTimeInCluster) {
                tree.CloseSequence();
                inCluster = false;
                return WriteBlock(blockDuration);
            }

            double endPosition = position + blockDuration;
            while (true) {
                double nextBlockTime = double.PositiveInfinity;
                int track = -1;
                for (int i = 0; i < tracks.Length; i++) {
                    if (tracks[i].IsNextBlockAvailable()) {
                        double offset = tracks[i].GetNextBlockOffset();
                        if (offset != -1 && nextBlockTime > offset) {
                            nextBlockTime = offset;
                            track = i;
                        }
                    }
                }
                if (track == -1 || nextBlockTime > endPosition) {
                    break;
                }

                bool keyframe = tracks[track].IsNextBlockKeyframe();
                if (keyframe && track == mainTrack && (cues.Count == 0 || cues[^1].Position != clusterOffset)) {
                    cues.Add(new Cue((long)(position * MatroskaReader.sToNs / timestampScale), track + 1, clusterOffset - segmentOffset));
                }
                short clusterOffsetTicks = (short)((nextBlockTime - lastClusterStarted) * MatroskaReader.sToNs / timestampScale);
                Block.Write(tree, writer, keyframe, track + 1, clusterOffsetTicks, tracks[track].ReadNextBlock());
            }

            position = endPosition;
            return position >= duration;
        }

        /// <summary>
        /// Close the last block and write the <see cref="cues"/>.
        /// </summary>
        public override void Dispose() {
            if (inCluster) {
                tree.CloseSequence();
            }
            CreateSegmentCues();
            tree.CloseSequence(); // Segment
            CreateSegmentSeekHead();
            base.Dispose();
        }

        /// <summary>
        /// Find the main video track and allocate it for generating <see cref="cues"/>.
        /// </summary>
        void SetMainTrack() {
            for (int i = 0; i < tracks.Length; i++) {
                if (tracks[i].Format.IsVideo()) {
                    mainTrack = i;
                    return;
                }
            }
        }

        /// <summary>
        /// Write the format header.
        /// </summary>
        void CreateEBMLHeader() {
            tree.OpenSequence(MatroskaTree.EBML, 1);
            tree.Write(MatroskaTree.EBML_Version, (byte)1);
            tree.Write(MatroskaTree.EBML_ReadVersion, (byte)1);
            tree.Write(MatroskaTree.EBML_MaxIDLength, (byte)4);
            tree.Write(MatroskaTree.EBML_MaxSizeLength, (byte)8);
            tree.Write(MatroskaTree.EBML_DocType, "matroska");
            tree.Write(MatroskaTree.EBML_DocTypeVersion, creatorVersion);
            tree.Write(MatroskaTree.EBML_DocTypeReadVersion, requiredVersion);
            tree.CloseSequence();
        }

        /// <summary>
        /// Overwrite the <see cref="MatroskaTree.EBML_Void"/> written as a placeholder for the seek header.
        /// </summary>
        void CreateSegmentSeekHead() {
            writer.Position = segmentOffset;
            tree.OpenSequence(MatroskaTree.Segment_SeekHead, 2);
            for (int i = 0, c = seekHead.Count; i < c; i++) {
                tree.OpenSequence(MatroskaTree.Segment_SeekHead_Seek, 1);
                tree.Write(MatroskaTree.Segment_SeekHead_Seek_SeekID, (uint)seekHead[i].tag);
                tree.Write(MatroskaTree.Segment_SeekHead_Seek_SeekPosition, (ulong)seekHead[i].offset);
                tree.CloseSequence();
            }
            tree.CloseSequence();
            tree.OpenSequence(MatroskaTree.EBML_Void, voidOverhead - 1);
            writer.Write(new byte[maxSeekHeadSize - (writer.Position - segmentOffset)]);
            tree.CloseSequence();
        }

        /// <summary>
        /// Write the informational part of the segment.
        /// </summary>
        void CreateSegmentInfo() {
            seekHead.Add((MatroskaTree.Segment_Info, writer.Position - segmentOffset));
            tree.OpenSequence(MatroskaTree.Segment_Info, 2);
            tree.Write(MatroskaTree.Segment_Info_TimestampScale, timestampScale);
            tree.Write(MatroskaTree.Segment_Info_MuxingApp, Listener.Info);
            tree.Write(MatroskaTree.Segment_Info_WritingApp, Listener.Info);
            tree.Write(MatroskaTree.Segment_Info_Duration, (float)(duration * MatroskaReader.sToNs / timestampScale));
            tree.CloseSequence();
        }

        /// <summary>
        /// Write the tracks of the segment.
        /// </summary>
        void CreateSegmentTracks() {
            seekHead.Add((MatroskaTree.Segment_Tracks, writer.Position - segmentOffset));
            tree.OpenSequence(MatroskaTree.Segment_Tracks, tracks.Length > 4 ? (byte)3 : (byte)2); // 4096 bytes per track is over the top
            for (int i = 0; i < tracks.Length;) {
                Track track = tracks[i++];
                bool audio = track.Format.IsAudio();
                tree.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry, 2);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackNumber, (short)i);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackUID, (short)i);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackType, audio ? (byte)2 : (byte)1);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_FlagLacing, (byte)0);
                if (!string.IsNullOrEmpty(track.Name)) {
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Name, track.Name);
                }
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Language, track.Language ?? "und");
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_CodecID, MatroskaTree.codecNames.GetKey(track.Format));
                if (audio) {
                    if (!(track.Extra is TrackExtraAudio audioInfo)) {
                        throw new MissingElementException(nameof(audioInfo));
                    }
                    tree.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Audio, 1);
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_SamplingFrequency, (float)audioInfo.SampleRate);
                    if (audioInfo.ChannelCount > 127) {
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels, (short)audioInfo.ChannelCount);
                    } else {
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels, (byte)audioInfo.ChannelCount);
                    }
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_BitDepth, (byte)audioInfo.Bits);
                    tree.CloseSequence();
                } else if (track.Extra is TrackExtraVideo videoInfo) {
                    if (videoInfo.FrameRate != 0) {
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_DefaultDuration,
                            (uint)(MatroskaReader.sToNs / videoInfo.FrameRate));
                    }
                    tree.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Video, 1);
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelWidth, (short)videoInfo.Width);
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelHeight, (short)videoInfo.Height);
                    if (videoInfo.ColorRange != ColorRange.Unspecified) {
                        tree.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour, 1);
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_Range, (byte)videoInfo.ColorRange);
                        tree.CloseSequence();
                    }
                    if (videoInfo is MatroskaTrackExtraVideo matroskaVideoInfo && matroskaVideoInfo.BlockAdditionMapping != null) {
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_BlockAdditionMapping, matroskaVideoInfo.BlockAdditionMapping);
                    }
                    tree.CloseSequence();
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_CodecPrivate, videoInfo.PrivateData);
                }
                tree.CloseSequence();
            }
            tree.CloseSequence();
        }

        /// <summary>
        /// Write the seeking offsets of the segment.
        /// </summary>
        void CreateSegmentCues() {
            seekHead.Add((MatroskaTree.Segment_Cues, writer.Position - segmentOffset));
            tree.OpenSequence(MatroskaTree.Segment_Cues, 4);
            for (int i = 0, c = cues.Count; i < c; i++) {
                cues[i].Write(tree);
            }
            tree.CloseSequence();
        }

        /// <summary>
        /// All mandatory values up to this version of Matroska will be included.
        /// </summary>
        const byte creatorVersion = 4;

        /// <summary>
        /// Matroska v2 (for its SimpleBlock support) is required for playback.
        /// </summary>
        const byte requiredVersion = 2;

        /// <summary>
        /// Matroska ticks per second.
        /// </summary>
        const int timestampScale = 1000000;

        /// <summary>
        /// Number of bytes to reserve for the seek header.
        /// </summary>
        const int maxSeekHeadSize = 150;

        /// <summary>
        /// Bytes of <see cref="maxSeekHeadSize"/> that are used by the key and length of the <see cref="MatroskaTree.EBML_Void"/>.
        /// </summary>
        const int voidOverhead = 2 /* size */ + 1 /* element ID */;

        /// <summary>
        /// The maximum seconds of content to write in a single cluster.
        /// </summary>
        const double maxTimeInCluster = 5;
    }
}