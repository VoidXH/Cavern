namespace Cavern.Format.Utilities {
    /// <summary>
    /// Common parsing utilities.
    /// </summary>
    public static class ParserExtensions {
        /// <summary>
        /// Convert a big-endian integer four-character code to characters.
        /// </summary>
        public static string ToFourCC(this uint tag) => new string(new[] {
            (char)(tag >> 24),
            (char)((tag >> 16) & 0xFF),
            (char)((tag >> 8) & 0xFF),
            (char)((tag) & 0xFF)
        });
    }
}