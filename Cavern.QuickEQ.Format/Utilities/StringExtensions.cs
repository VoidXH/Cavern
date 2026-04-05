using System.Collections.Generic;
using System.IO;

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
        /// Get if the string is made of the same character, and if so, which one. Returns 0 if the string is empty or contains different characters.
        /// </summary>
        public static char IsTheSameCharacter(this string str) {
            if (str.Length == 0) {
                return '\0';
            }

            char start = str[0];
            for (int i = 1; i < str.Length; i++) {
                if (str[i] != start) {
                    return '\0';
                }
            }
            return start;
        }

        /// <summary>
        /// Reads a string line by line with very little memory overhead.
        /// </summary>
        public static IEnumerable<string> ReadLines(this string source) {
            using StringReader reader = new StringReader(source);
            string line;
            while ((line = reader.ReadLine()) != null) {
                yield return line;
            }
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
