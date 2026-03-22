using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Cavern.QuickEQ.Graphing;

namespace Cavern.WPF.Utils {
    /// <summary>
    /// Extension functions for interoperability between Cavern's raw ARGB pixel arrays and WPF's <see cref="Bitmap"/>.
    /// </summary>
    public static partial class BitmapUtils {
        /// <summary>
        /// Convert an array of ARGB pixels to a <see cref="BitmapSource"/> that can be used in WPF.
        /// </summary>
        public static BitmapSource ToBitmapSource(this GraphRenderer renderer) => renderer.Pixels.ToBitmapSource(renderer.Width, renderer.Height);

        /// <summary>
        /// Convert an array of ARGB pixels to a <see cref="BitmapSource"/> that can be used in WPF.
        /// </summary>
        public static BitmapSource ToBitmapSource(this uint[] argb, int width, int height) {
            PixelFormat format = PixelFormats.Bgra32;
            byte[] pixelData = new byte[4 * width * height];
            for (int y = 0; y < height; y++) {
                int stride = 4 * width * y;
                for (int x = 0; x < width; x++) {
                    uint pixelValue = argb[(height - y - 1) * width + x];
                    int pos = stride + x * 4;
                    pixelData[pos] = (byte)pixelValue;
                    pixelData[pos + 1] = (byte)(pixelValue >> 8);
                    pixelData[pos + 2] = (byte)(pixelValue >> 16);
                    pixelData[pos + 3] = (byte)(pixelValue >> 24);
                }
            }

            return BitmapSource.Create(width, height, 96, 96, format, null, pixelData, 4 * width);
        }

        /// <summary>
        /// Convert a <see cref="BitmapSource"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        public static BitmapImage ToBitmapImage(this BitmapSource source) {
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            result.StreamSource = GetMemoryStreamFromBitmapSource(source);
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.EndInit();
            return result;
        }

        /// <summary>
        /// Allow loading a virtual <param name="source"> image from a <see cref="Stream"/>.
        /// </summary>
        static MemoryStream GetMemoryStreamFromBitmapSource(BitmapSource source) {
            MemoryStream stream = new MemoryStream();
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            return stream;
        }
    }
}
