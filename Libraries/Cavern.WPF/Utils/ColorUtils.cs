using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Cavern.WPF.Utils {
    /// <summary>
    /// Extension functions for interoperability between Cavern's raw ARGB pixel arrays and WPF's <see cref="Color"/>.
    /// </summary>
    public static class ColorUtils {
        /// <summary>
        /// Convert a 32-bit ARGB value to WPF's <see cref="Color"/> used in, for example, <see cref="Brush"/>es.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToColor(uint argb) => Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }
}