using System;

namespace Cavern.Format.Common {
    /// <summary>
    /// Codecs detected (not neccessarily supported) by Cavern.Format.
    /// Video codecs come before audio, and higher quality (newer, immersive, better overall quality) codecs also come first.
    /// </summary>
    public enum Codec {
        /// <summary>
        /// Undetected codec.
        /// </summary>
        Unknown,
        /// <summary>
        /// High Efficiency Video Coding aka H.265, video.
        /// </summary>
        HEVC,
        /// <summary>
        /// <see cref="HEVC"/> containing Dolby Vision, video.
        /// </summary>
        HEVC_DolbyVision,
        /// <summary>
        /// Advanced Video Coding aka H.264, video.
        /// </summary>
        AVC,
        /// <summary>
        /// Limitless Audio Format, audio.
        /// </summary>
        LimitlessAudio,
        /// <summary>
        /// Audio Definition Model Broadcast Wave Format, audio.
        /// </summary>
        ADM_BWF,
        /// <summary>
        /// Audio Definition Model Broadcast Wave Format - Dolby Atmos subset, audio.
        /// </summary>
        ADM_BWF_Atmos,
        /// <summary>
        /// Dolby Atmos Master Format, audio.
        /// </summary>
        DAMF,
        /// <summary>
        /// Dolby TrueHD (Meridian Lossless Packing), audio.
        /// </summary>
        TrueHD,
        /// <summary>
        /// Enhanced AC-3 (Dolby Digital Plus), audio.
        /// </summary>
        EnhancedAC3,
        /// <summary>
        /// Pulse Code Modulation, IEEE floating point, audio.
        /// </summary>
        PCM_Float,
        /// <summary>
        /// Pulse Code Modulation, little-endian integer, audio.
        /// </summary>
        PCM_LE,
        /// <summary>
        /// DTS-HD lossless, could be DTS:X, audio.
        /// </summary>
        DTS_HD,
        /// <summary>
        /// Xiph Free Lossless Audio Codec, audio.
        /// </summary>
        FLAC,
        /// <summary>
        /// Xiph Opus, audio.
        /// </summary>
        Opus,
        /// <summary>
        /// DTS, could be any DTS format, audio.
        /// </summary>
        DTS,
        /// <summary>
        /// AC-3 (Dolby Digital), audio.
        /// </summary>
        AC3,
    }
}
