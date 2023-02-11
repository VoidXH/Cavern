using System;
using System.Globalization;
using System.Windows.Data;

namespace VoidX.WPF {
    /// <summary>
    /// Converts a camel-case enum with spaces and uppercase initial.
    /// </summary>
    public class EnumToTitleCase : IValueConverter {
        /// <summary>
        /// Converts a camel-case string with spaces and uppercase initial.
        /// </summary>
        public static string GetTitleCase(string source) {
            if (string.IsNullOrEmpty(source)) {
                return null;
            }
            char[] target = new char[source.Length * 2];
            bool lastCamel = false;
            int size = 1;
            target[0] = source[0] >= 'a' && source[0] <= 'z' ? (char)(source[0] + ('A' - 'a')) : source[0];
            for (int i = 1; i < source.Length; i++) {
                if (source[i] >= 'A' && source[i] <= 'Z') {
                    if (!lastCamel) {
                        target[size++] = ' ';
                        lastCamel = true;
                    }
                    target[size++] = (char)(source[i] - ('A' - 'a'));
                } else {
                    lastCamel = false;
                    target[size++] = source[i];
                }
            }
            return new(target, 0, size);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            targetType == typeof(string) && value != null ? GetTitleCase(value.ToString()) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}