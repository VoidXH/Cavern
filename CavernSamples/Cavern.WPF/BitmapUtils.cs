using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;

using Image = System.Windows.Controls.Image;

namespace Cavern.WPF {
#pragma warning disable CA1416 // Validate platform compatibility
    /// <summary>
    /// Extension functions for interoperability between Cavern's raw ARGB pixel arrays and WPF's <see cref="Bitmap"/>.
    /// </summary>
    public static class BitmapUtils {
        /// <summary>
        /// Convert an array of ARGB pixels to a <see cref="Bitmap"/>.
        /// </summary>
        public static Bitmap ToBitmap(this uint[] argb, int width, int height) {
            Bitmap output = new Bitmap(width, height);
            height--;
            for (int y = 0, i = 0; y <= height; y++) {
                for (int x = 0; x < width; x++) {
                    output.SetPixel(x, height - y, Color.FromArgb((int)argb[i++]));
                }
            }
            return output;
        }

        /// <summary>
        /// Convert a <see cref="Bitmap"/> to a format usable as a source for an <see cref="Image"/>.
        /// </summary>
        public static BitmapSource ToImageSource(Bitmap bitmap) {
            IntPtr handle = bitmap.GetHbitmap();
            try {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            } finally {
                DeleteObject(handle);
            }
        }

        /// <summary>
        /// Free up a Hbitmap after a conversion is finished.
        /// </summary>
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject([In] IntPtr hObject);
    }
#pragma warning restore CA1416 // Validate platform compatibility
}