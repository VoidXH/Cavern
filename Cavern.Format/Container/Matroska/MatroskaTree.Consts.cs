using System.Collections.Generic;

using Cavern.Format.Common;

namespace Cavern.Format.Container.Matroska {
    partial class MatroskaTree {
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
            Segment_Tracks_TrackEntry_Video_Colour_MaxCLL = 0x55BC,
            Segment_Tracks_TrackEntry_Video_Colour_MaxFALL = 0x55BD,
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

        /// <summary>
        /// This element on the Matroska tree has further leaves.
        /// </summary>
        /// <remarks>Uses a switch for performance.</remarks>
        public bool HasChildren() => Tag switch {
            Segment_Cluster_BlockGroup => true,
            Segment_Tracks_TrackEntry => true,
            Segment_Cues_CuePoint => true,
            Segment_Tracks_TrackEntry_Video => true,
            Segment_Tracks_TrackEntry_Audio => true,
            Segment_SeekHead_Seek => true,
            Segment_SeekHead => true,
            Segment_Info => true,
            Segment_Tracks => true,
            Segment => true,
            EBML => true,
            _ => false,
        };
    }
}
