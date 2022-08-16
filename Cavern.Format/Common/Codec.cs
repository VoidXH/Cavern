using System;

namespace Cavern.Format.Common {
    /// <summary>
    /// Codecs detected (not neccessarily supported) by Cavern.Format.
    /// Video codecs come befpre audio, and higher quality (newer, immersive, better overall quality) codecs also come first.
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
        /// Advanced Video Coding aka H.264, video.
        /// </summary>
        AVC,
        /// <summary>
        /// Audio Definition Model Broadcast Wave Format, audio.
        /// </summary>
        ADM_BWF,
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
        /// Dolby TrueHD (Meridian Lossless Packaging), audio.
        /// </summary>
        // TODO: move to first place when objects can be decoded, otherwise think of this as a simple 7.1 codec
        TrueHD,
        /// <summary>
        /// DTS-HD lossless, could be DTS:X, audio.
        /// </summary>
        DTS_HD,
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

    /// <summary>
    /// Extra functions for codecs.
    /// </summary>
    public static class CodecExtensions {
        /// <summary>
        /// List of known (not neccessarily supported) audio codecs.
        /// </summary>
        static readonly Codec[] audioCodecs = { Codec.ADM_BWF, Codec.EnhancedAC3, Codec.PCM_Float, Codec.PCM_LE, Codec.TrueHD,
            Codec.DTS_HD, Codec.Opus, Codec.DTS, Codec.AC3 };

        /// <summary>
        /// List of supported audio codecs.
        /// </summary>
        static readonly Codec[] supportedAudioCodecs = { Codec.ADM_BWF, Codec.EnhancedAC3,
            Codec.PCM_Float, Codec.PCM_LE, Codec.AC3 };

        /// <summary>
        /// Checks if a codec is used for audio only.
        /// </summary>
        public static bool IsAudio(this Codec codec) => Array.BinarySearch(audioCodecs, codec) >= 0;

        /// <summary>
        /// Checks if a codec is a supported audio codec.
        /// </summary>
        public static bool IsSupportedAudio(this Codec codec) => Array.BinarySearch(supportedAudioCodecs, codec) >= 0;
    }
}