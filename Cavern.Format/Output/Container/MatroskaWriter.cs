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
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(Stream writer, Track[] tracks) : base(writer, tracks) { }

        /// <summary>
        /// Writes source <paramref name="tracks"/> to a Matroska file.
        /// </summary>
        public MatroskaWriter(string path, Track[] tracks) : base(path, tracks) { }

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public override void WriteHeader() {
            MatroskaTreeWriter tree = new MatroskaTreeWriter(writer);
            CreateEBMLHeader(tree);
            tree.OpenSequence(MatroskaTree.Segment, 5); // Maximum file size supported is 69 GB (nice)
            CreateSegmentInfo(tree);
            CreateSegmentTracks(tree);
            tree.CloseSequence();
        }

        /// <summary>
        /// Write the format header.
        /// </summary>
        void CreateEBMLHeader(MatroskaTreeWriter tree) {
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
        void CreateSegmentInfo(MatroskaTreeWriter tree) {
            tree.OpenSequence(MatroskaTree.Segment_Info, 2);
            tree.Write(MatroskaTree.Segment_Info_TimestampScale, timestampScale);
            tree.Write(MatroskaTree.Segment_Info_MuxingApp, Listener.Info);
            tree.Write(MatroskaTree.Segment_Info_WritingApp, Listener.Info);
            // TODO: duration (max of tracks)
            tree.CloseSequence();
        }

        /// <summary>
        /// Write the tracks of the segment.
        /// </summary>
        void CreateSegmentTracks(MatroskaTreeWriter tree) {
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
                        tree.Write(MatroskaTree.Segment_Tracks_TrackEntry_DefaultDuration, (uint)(1000000000 / videoInfo.FrameRate));
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