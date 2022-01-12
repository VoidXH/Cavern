namespace Cavern.Format {
    /// <summary>
    /// Used for both <see cref="LimitlessAudioFormatReader"/> and <see cref="LimitlessAudioFormatWriter"/>.
    /// </summary>
    internal static class LimitlessAudioFormatUtils {
        /// <summary>
        /// Limitless Audio Format indicator starting bytes.
        /// </summary>
        public static readonly byte[] limitless =
            new byte[9] { (byte)'L', (byte)'I', (byte)'M', (byte)'I', (byte)'T', (byte)'L', (byte)'E', (byte)'S', (byte)'S' };

        /// <summary>
        /// Header marker bytes.
        /// </summary>
        public static readonly byte[] head = new byte[4] { (byte)'H', (byte)'E', (byte)'A', (byte)'D' };
    }
}