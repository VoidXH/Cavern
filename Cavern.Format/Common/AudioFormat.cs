namespace Cavern.Format {
    /// <summary>
    /// Supported audio formats in this namespace.
    /// </summary>
    public enum AudioFormat {
        /// <summary>
        /// Minimal RIFF Wave implementation.
        /// </summary>
        RIFFWave,
        /// <summary>
        /// Limitless Audio Format, supports spatial mixes.
        /// </summary>
        LimitlessAudioFormat,
    }

    /// <summary>
    /// Audio bit depth choices.
    /// </summary>
    public enum BitDepth {
        /// <summary>
        /// 8-bit integer.
        /// </summary>
        Int8 = 8,
        /// <summary>
        /// 16-bit integer.
        /// </summary>
        Int16 = 16,
        /// <summary>
        /// 24-bit integer.
        /// </summary>
        Int24 = 24,
        /// <summary>
        /// 32-bit floating point.
        /// </summary>
        Float32 = 32,
    }

    /// <summary>
    /// Limitless Audio Format quality modes.
    /// </summary>
    public enum LAFMode {
        /// <summary>
        /// 8-bit integer.
        /// </summary>
        Int8 = 0,
        /// <summary>
        /// 16-bit integer.
        /// </summary>
        Int16 = 1,
        /// <summary>
        /// 24-bit integer.
        /// </summary>
        Int24 = 3,
        /// <summary>
        /// 32-bit floating point.
        /// </summary>
        Float32 = 2,
    }

    internal static class BitConversions {
        public const int int24Max = (1 << 23) - 1;
        public const float fromInt8 = 1f / sbyte.MaxValue;
        public const float fromInt16 = 1f / short.MaxValue;
        public const float fromInt24 = 1f / int24Max;
        public const float fromInt32 = 1f / int.MaxValue;
    }
}