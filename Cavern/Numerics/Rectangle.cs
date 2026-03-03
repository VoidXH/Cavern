namespace Cavern.Numerics {
    /// <summary>
    /// A rectangle defined by its top left corner and its width and height.
    /// </summary>
    public struct Rectangle {
        /// <summary>
        /// Horizontal position of the top left corner of the rectangle in pixels.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Vertical position of the top left corner of the rectangle in pixels.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Width of the rectangle in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the rectangle in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// A rectangle defined by its top left corner and its width and height.
        /// </summary>
        public Rectangle(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
