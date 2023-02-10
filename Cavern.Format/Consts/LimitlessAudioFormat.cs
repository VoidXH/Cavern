namespace Cavern.Format.Consts {
    /// <summary>
    /// Used for both <see cref="LimitlessAudioFormatReader"/> and <see cref="LimitlessAudioFormatWriter"/>.
    /// </summary>
    internal static class LimitlessAudioFormat {
        /// <summary>
        /// First half of the LIMITLESS word as a single int for quick format detection.
        /// </summary>
        public const int syncWord = 0x494D494C;

        /// <summary>
        /// Limitless Audio Format indicator starting bytes.
        /// </summary>
        public static readonly byte[] limitless =
            { (byte)'L', (byte)'I', (byte)'M', (byte)'I', (byte)'T', (byte)'L', (byte)'E', (byte)'S', (byte)'S' };

        /// <summary>
        /// Header marker bytes.
        /// </summary>
        public static readonly byte[] head = { (byte)'H', (byte)'E', (byte)'A', (byte)'D' };
    }
}