using Cavern.Format.Common;
using Cavern.Format.Common.Metadata;
using Cavern.Format.Common.Metadata.Enums;
using Cavern.Utilities;

namespace Cavern.Format.Container.Matroska {
    /// <summary>
    /// Handles writing metadata of <see cref="tracks"/> to a Matroska file, including writing the Tracks segment itself.
    /// </summary>
    class MatroskaTrackWriter {
        /// <summary>
        /// Track metadata to write to the Tracks segment.
        /// </summary>
        readonly Track[] tracks;

        /// <summary>
        /// Handles writing metadata of <paramref name="tracks"/> to a Matroska file, including writing the Tracks segment itself.
        /// </summary>
        public MatroskaTrackWriter(Track[] tracks) => this.tracks = tracks;

        /// <summary>
        /// Append the Tracks segment to the Matroska file under construction.
        /// </summary>
        public void Write(MatroskaTreeWriter writer) {
            writer.OpenSequence(MatroskaTree.Segment_Tracks, tracks.Length > 4 ? (byte)3 : (byte)2); // 4096 bytes per track is over the top
            for (int i = 0; i < tracks.Length; i++) {
                WriteTrack(writer, tracks[i], i);
            }
            writer.CloseSequence();
        }

        /// <summary>
        /// Append a single track to the Matroska Tracks segment under construction.
        /// </summary>
        void WriteTrack(MatroskaTreeWriter writer, Track track, int index) {
            bool audio = track.Format.IsAudio();
            writer.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry, 2);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackNumber, (ushort)index);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackUID, (ushort)index);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_TrackType, audio ? (byte)2 : (byte)1);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_FlagLacing, (byte)0);
            if (!string.IsNullOrEmpty(track.Name)) {
                writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Name, track.Name);
            }
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Language, track.Language ?? "und");
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_CodecID, MatroskaTree.codecNames.GetKey(track.Format));
            if (audio) {
                WriteAudioMetadata(writer, track);
            } else {
                WriteVideoMetadata(writer, track);
            }
            writer.CloseSequence();
        }

        /// <summary>
        /// Append an audio track's mandatory and optional metadata to a track segment under construction.
        /// </summary>
        void WriteAudioMetadata(MatroskaTreeWriter writer, Track track) {
            if (!(track.Extra is TrackExtraAudio audioInfo)) {
                throw new MissingElementException(nameof(audioInfo));
            }
            writer.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Audio, 1);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_SamplingFrequency, (float)audioInfo.SampleRate);
            if (audioInfo.ChannelCount > 127) {
                writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels, (ushort)audioInfo.ChannelCount);
            } else {
                writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_Channels, (byte)audioInfo.ChannelCount);
            }
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Audio_BitDepth, (byte)audioInfo.Bits);
            writer.CloseSequence();
        }

        /// <summary>
        /// Append a video track's mandatory and optional metadata to a track segment under construction.
        /// </summary>
        void WriteVideoMetadata(MatroskaTreeWriter writer, Track track) {
            if (!(track.Extra is TrackExtraVideo videoInfo)) {
                return;
            }
            if (videoInfo.FrameRate != 0) {
                writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_DefaultDuration,
                    (uint)(MatroskaReader.sToNs / videoInfo.FrameRate));
            }
            writer.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Video, 1);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelWidth, (ushort)videoInfo.Width);
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_PixelHeight, (ushort)videoInfo.Height);

            ColorMetadata color = videoInfo.ColorMetadata;
            if (color != null) {
                writer.OpenSequence(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour, 1);
                if (color.ColorRange != ColorRange.Unspecified) {
                    writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_Range, (byte)color.ColorRange);
                }
                if (color.MaxCLL != 0) {
                    writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_MaxCLL, (ushort)color.MaxCLL);
                }
                if (color.MaxFALL != 0) {
                    writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_Video_Colour_MaxFALL, (ushort)color.MaxFALL);
                }
                writer.CloseSequence();
            }
            if (videoInfo is MatroskaTrackExtraVideo matroskaVideoInfo && matroskaVideoInfo.BlockAdditionMapping != null) {
                writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_BlockAdditionMapping, matroskaVideoInfo.BlockAdditionMapping);
            }
            writer.CloseSequence();
            writer.Write(MatroskaTree.Segment_Tracks_TrackEntry_CodecPrivate, videoInfo.PrivateData);
        }
    }
}
