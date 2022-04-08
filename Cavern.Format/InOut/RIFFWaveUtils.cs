namespace Cavern.Format.InOut {
    /// <summary>
    /// Used for both <see cref="RIFFWaveReader"/> and <see cref="RIFFWaveWriter"/>.
    /// </summary>
    internal static class RIFFWaveUtils {
        /// <summary>
        /// RIFF sync word, stream marker.
        /// </summary>
        public const int syncWord1 = 0x46464952;

        /// <summary>
        /// WAVE and fmt sync word, header marker.
        /// </summary>
        public const long syncWord2 = 0x20746D6645564157;

        /// <summary>
        /// Data header marker (big-endian).
        /// </summary>
        public const int syncWord3BE = 0x64617461;

        /// <summary>
        /// Data header marker (little-endian).
        /// </summary>
        public const int syncWord3LE = 0x61746164;
    }
}