namespace Cavern.Format.Consts {
    /// <summary>
    /// Shorthands for integer sample handling.
    /// </summary>
    internal static class BitConversions {
        /// <summary>
        /// Maximum value in a 24-bit integer sample.
        /// </summary>
        public const int int24Max = (1 << 23) - 1;

        /// <summary>
        /// Convert an 8-bit integer sample to float.
        /// </summary>
        public const float fromInt8 = 1f / sbyte.MaxValue;

        /// <summary>
        /// Convert a 16-bit integer sample to float.
        /// </summary>
        public const float fromInt16 = 1f / short.MaxValue;

        /// <summary>
        /// Convert a 24-bit integer sample to float.
        /// </summary>
        public const float fromInt24 = 1f / int24Max;

        /// <summary>
        /// Convert a 32-bit integer sample to float.
        /// </summary>
        public const float fromInt32 = 1f / int.MaxValue;
    }
}
