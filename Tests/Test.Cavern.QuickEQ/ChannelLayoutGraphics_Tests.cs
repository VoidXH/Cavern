using Cavern;
using Cavern.QuickEQ.Graphing;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="ChannelLayoutGraphics"/> class.
    /// </summary>
    [TestClass]
    public class ChannelLayoutGraphics_Tests {
        /// <summary>
        /// Tests that the connection frame stays within the image even when the connection lines are wider
        /// than the channel squares, which used to produce a negative padding and an
        /// <see cref="System.ArgumentOutOfRangeException"/> from <c>DrawingUtils.AddRectangle</c>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ConnectionFrameStaysInBounds() {
            ChannelLayoutGraphics graphics = new() {
                Width = 32,
                Height = 32,
                ChannelWidth = 8,
                ConnectionWidth = 16
            };
            uint[] image = graphics.Draw(new[] { new Channel(0, 0) });
            Assert.AreEqual(graphics.Width * graphics.Height, image.Length);
        }

        /// <summary>
        /// Tests that drawing a layout never throws for a wide range of elevations and image sizes.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void DrawIsStable() {
            foreach (int size in new[] { 8, 16, 32, 64, 512 }) {
                foreach (float elevation in new[] { -90, -45, 0, 45, 90, 120 }) {
                    ChannelLayoutGraphics graphics = new() { Width = size, Height = size };
                    graphics.Draw(new[] { new Channel(elevation, 0) });
                }
            }
        }
    }
}
