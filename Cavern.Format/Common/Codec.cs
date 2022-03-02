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
        /// High Efficiency Video Coding aka H.265, video.
        /// </summary>
        HEVC,
        /// <summary>
        /// Opus, audio.
        /// </summary>
        Opus
    }
}