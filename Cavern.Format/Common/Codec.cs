using System;

namespace Cavern.Format.Common {
    /// <summary>
    /// Codecs detected (not supported) by Cavern.Format.
    /// </summary>
    public enum Codec {
        /// <summary>
        /// Undetected codec.
        /// </summary>
        Unknown,
        /// <summary>
        /// Advanced Video Coding aka H.264, video.
        /// </summary>
        AVC,
        /// <summary>
        /// High Efficiency Video Coding aka H.265, video.
        /// </summary>
        HEVC,
        /// <summary>
        /// DTS, could be any DTS format, audio.
        /// </summary>
        DTS,
        /// <summary>
        /// DTS-HD lossless, could be DTS:X, audio.
        /// </summary>
        DTS_HD,
        /// <summary>
        /// Opus, audio.
        /// </summary>
        Opus,
        /// <summary>
        /// Pulse Code Modulation, IEEE floating point, audio.
        /// </summary>
        PCM_Float,
        /// <summary>
        /// Pulse Code Modulation, little-endian integer, audio.
        /// </summary>
        PCM_LE
    }

    /// <summary>
    /// Extra functions for codecs.
    /// </summary>
    public static class CodecExtensions {
        /// <summary>
        /// List of known (not neccessarily supported) audio codecs.
        /// </summary>
        static readonly Codec[] audioCodecs = { Codec.DTS, Codec.DTS_HD, Codec.Opus, Codec.PCM_Float, Codec.PCM_LE };

        /// <summary>
        /// Checks if a codec is used for audio only.
        /// </summary>
        public static bool IsAudio(this Codec codec) => Array.BinarySearch(audioCodecs, codec) >= 0;
    }
}