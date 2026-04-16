using System.IO;
using System.Text;

namespace Cavern.Format.Image {
    /// <summary>
    /// BMP image format handler.
    /// </summary>
    public class Bitmap : Image {
        /// <summary>
        /// BMP image format handler.
        /// </summary>
        public Bitmap(int width, int height) : base(width, height) { }

        /// <summary>
        /// BMP image format handler.
        /// </summary>
        public Bitmap(int width, int height, uint[] argb) : base(width, height, argb) { }

        /// <inheritdoc/>
        public override void Write(Stream output) {
            using BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            int pixelDataSize = Width * Height * 4;

            // Bitmap header
            writer.Write((ushort)0x4D42); // Magic word ("BM")
            writer.Write(14 + 40 + pixelDataSize);
            writer.Write(0);              // Reserved
            writer.Write(14 + 40);        // Offset to pixel data

            // DIB header
            writer.Write(40);             // Header size
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((ushort)1);      // Planes
            writer.Write((ushort)32);     // Bits per pixel (8-bit ARGB)
            writer.Write(0);              // Compression (0 = BI_RGB)
            writer.Write(pixelDataSize);
            writer.Write(0);              // X pixels per meter
            writer.Write(0);              // Y pixels per meter
            writer.Write(0);              // Colors in palette
            writer.Write(0);              // Important colors

            // Pixel data
            for (int i = 0; i < Pixels.Length; i++) {
                writer.Write(Pixels[i]);
            }
        }
    }
}
