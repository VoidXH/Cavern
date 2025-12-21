namespace Cavern.Format.Utilities {
    /// <summary>
    /// String operations not supported by .NET.
    /// </summary>
    public static class StringExtensions {
        /// <summary>
        /// Convert special characters to their escaped versions.
        /// </summary>
        public static string Escape(this string str) {
            for (int i = 0; i < escapes.Length; i++) {
                string chr = escapes[i].chr;
                if (str.Contains(chr)) {
                    str = str.Replace(chr, escapes[i].escaped);
                }
            }
            return str;
        }

        /// <summary>
        /// Convert back escaped strings.
        /// </summary>
        public static string Unescape(this string str) {
            for (int i = 0; i < escapes.Length; i++) {
                string escaped = escapes[i].escaped;
                if (str.Contains(escaped)) {
                    str = str.Replace(escaped, escapes[i].chr);
                }
            }
            return str;
        }

        /// <summary>
        /// Which characters escape to what strings.
        /// </summary>
        static readonly (string chr, string escaped)[] escapes = {
            ("\\", "\\\\"), // Handle this first
            ("\0", "\\0"),
            ("\'", "\\\'"),
            ("\"", "\\\""),
            ("\n", "\\\n"),
            ("\r", "\\\r"),
            ("\t", "\\\t"),
        };
    }
}
