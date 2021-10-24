using System.Globalization;
using System.Text;

namespace HRTFSetImporter {
    static class StringBuilderExtensions {
        static readonly NumberFormatInfo numberFormat = new NumberFormatInfo {
            NumberDecimalSeparator = "."
        };

        public static StringBuilder AppendArray(this StringBuilder builder, float[] array) {
            builder.Append("new float[").Append(array.Length).Append("] { ");
            for (int i = 0; i < array.Length; ++i)
                builder.Append(array[i].ToString(numberFormat)).Append("f, ");
            builder.Remove(builder.Length - 2, 2).AppendLine(" },");
            return builder;
        }
    }
}