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
}