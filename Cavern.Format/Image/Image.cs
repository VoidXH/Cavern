using System.IO;

namespace Cavern.Format.Image {
    /// <summary>
    /// Holds pixel data that is parsed from and/or parsable to an image codec.
    /// </summary>
    public abstract class Image {
        /// <summary>
        /// Width of the image in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the image in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Array of ARGB pixels representing the image.
        /// </summary>
        public uint[] Pixels { get; }

        /// <summary>
        /// Holds pixel data that is parsed from and/or parsable to an image codec.
        /// </summary>
        protected Image(int width, int height) {
            Width = width;
            Height = height;
            Pixels = new uint[Width * Height];
        }

        /// <summary>
        /// Holds pixel data that is parsed from and/or parsable to an image codec.
        /// </summary>
        protected Image(int width, int height, uint[] argb) {
            Width = width;
            Height = height;
            Pixels = argb;
        }

        /// <summary>
        /// Encode the image to an <paramref name="output"/> <see cref="Stream"/>.
        /// </summary>
        public abstract void Write(Stream output);

        /// <summary>
        /// Export the image file to a target <paramref name="path"/>.
        /// </summary>
        public void Write(string path) {
            using FileStream stream = File.OpenWrite(path);
            Write(stream);
        }
    }
}
