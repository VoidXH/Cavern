using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Functions for common encoding and decoding tasks.
    /// </summary>
    public static class EncodingUtils {
        /// <summary>
        /// Encode a float array to a base 64 string.
        /// </summary>
        public static string ToBase64(float[] source) {
            if (source == null || source.Length == 0) {
                return string.Empty;
            }

            byte[] result = new byte[source.Length * sizeof(float)];
            Buffer.BlockCopy(source, 0, result, 0, result.Length);
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decode a base 64-encoded float array.
        /// </summary>
        public static float[] Base64ToFloatArray(string source) {
            if (string.IsNullOrEmpty(source)) {
                return new float[0];
            }

            byte[] from = Convert.FromBase64String(source);
            float[] result = new float[from.Length / sizeof(float)];
            Buffer.BlockCopy(from, 0, result, 0, from.Length);
            return result;
        }
    }
}