using System;
using System.Globalization;
using System.Text;

namespace HRTFSetImporter {
    static class StringBuilderExtensions {
        public static StringBuilder AppendArray(this StringBuilder builder, float[] array) {
            builder.Append("new float[").Append(array.Length).Append("] { ");
            for (int i = 0; i < array.Length; ++i) {
                string str = array[i].ToString(CultureInfo.InvariantCulture);
                if (str.StartsWith("0.")) {
                    builder.Append(str.AsSpan(1)).Append("f, ");
                } else if (str.Equals("0")) {
                    builder.Append("0, ");
                } else {
                    builder.Append(str).Append("f, ");
                }
            }
            builder.Remove(builder.Length - 2, 2).AppendLine(" },");
            return builder;
        }
    }
}