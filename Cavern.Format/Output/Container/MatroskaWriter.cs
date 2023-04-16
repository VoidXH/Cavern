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
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(Stream writer, Track[] tracks, double duration) : base(writer, tracks, duration) { }

        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(string path, Track[] tracks, double duration) : base(path, tracks, duration) { }

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public override void WriteHeader() {
            tree = new MatroskaTreeWriter(writer);
            CreateEBMLHeader();
            tree.OpenSequence(MatroskaTree.Segment, 5); // Maximum file size supported is 69 GB (nice)
            CreateSegmentInfo();
            CreateSegmentTracks();
        }

        /// <summary>
        /// Write the frames that are part of the next block with of a given <see cref="duration"/>.
        /// </summary>
        /// <returns>The writing has finished.</returns>
        public override bool WriteBlock(double blockDuration) {
            if (!inCluster) {
                inCluster = true;
                lastClusterStarted = position;
                tree.OpenSequence(MatroskaTree.Segment_Cluster, 4);
                uint pos = (uint)(position * MatroskaReader.sToNs / timestampScale);
                if (pos < short.MaxValue) {
                    tree.Write(MatroskaTree.Segment_Cluster_Timestamp, (short)pos);
                } else {
                    tree.Write(MatroskaTree.Segment_Cluster_Timestamp, pos);
                }
            }

            uint relativeTimestamp = (uint)((position - lastClusterStarted) * MatroskaReader.sToNs / timestampScale);
            if (relativeTimestamp > short.MaxValue) { // Start a new cluster when the available bits for offset are exhausted
                tree.CloseSequence();
                inCluster = false;
                return WriteBlock(blockDuration);
            }

            double endPosition = position + blockDuration;
            while (true) {
                double nextBlockTime = double.PositiveInfinity;
                int nextBlock = -1;
                for (int i = 0; i < tracks.Length; i++) {
                    double offset = tracks[i].GetNextBlockOffset();
                    if (offset != -1 && nextBlockTime > offset) {
                        nextBlockTime = offset;
                        nextBlock = i;
                    }
                }
                if (nextBlock == -1 || nextBlockTime > endPosition) {
                    break;
                }

                bool keyframe = tracks[nextBlock].IsNextBlockKeyframe();
                short clusterOffset = (short)((nextBlockTime - lastClusterStarted) * MatroskaReader.sToNs / timestampScale);
                Block.Write(tree, writer, keyframe, nextBlock + 1, clusterOffset, tracks[nextBlock].ReadNextBlock());
            }

            position = endPosition;
            return position >= duration;
        }

        /// <summary>
        /// Close the last block and write the cues.
        /// </summary>
        public override void Dispose() {
            if (inCluster) {
                tree.CloseSequence();
            }
            tree.CloseSequence(); // Segment
            base.Dispose();
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
        /// Write the informational part of the segment.
        /// </summary>
        void CreateSegmentInfo() {
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
            tree.OpenSequence(MatroskaTree.Segment_Tracks, tracks.Length > 4 ? (byte)3 : (byte)2); // 4096 bytes per track is over the top
            for (int i = 0; i < tracks.Length;) {
                Track track = tracks[i++];
                bool audio = track.Format.IsAudio();
                tree.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry, 2);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackNumber, (short)i);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackUID, (short)i);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackType, audio ? (byte)2 : (byte)1);
                tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_FlagLacing, (byte)0);
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
                    tree.CloseSequence();
                    tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_CodecPrivate, videoInfo.PrivateData);
                }
                tree.CloseSequence();
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
    }
}