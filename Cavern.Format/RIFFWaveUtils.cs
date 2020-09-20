namespace Cavern.Format {
    /// <summary>Used for both <see cref="RIFFWaveReader"/> and <see cref="RIFFWaveWriter"/>.</summary>
    internal static class RIFFWaveUtils {
        /// <summary>RIFF marker.</summary>
        public static byte[] RIFF = new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' };
        /// <summary>WAVE marker.</summary>
        public static byte[] WAVE = new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' };
        /// <summary>Format chunk marker.</summary>
        public static byte[] fmt = new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' };
        /// <summary>Data chunk marker.</summary>
        public static byte[] data = new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
    }
}