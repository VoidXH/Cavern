using Cavern.QuickEQ;
using Cavern.QuickEQ.Equalization;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="Windowing"/> class.
    /// </summary>
    [TestClass]
    public class Windowing_Tests {
        /// <summary>
        /// Tests if the Hann window produces an expected result for a mono signal.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void HannMono() {
            float[] window = new float[hannResult.Length];
            Array.Fill(window, 1);
            Windowing.ApplyWindow(window, Window.Hann);
            CollectionAssert.AreEqual(window, hannResult);
        }

        /// <summary>
        /// Tests if the Hann window produces an expected result for an.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void HannEqualizer() {
            Equalizer eq = new();
            int half = hannResult.Length >> 1;
            for (int i = half; i < hannResult.Length; i++) {
                eq.AddBand(new Band(i, 1));
            }
            eq.Window(Window.Hann, half, hannResult.Length);
            for (int i = 0; i < half; i++) {
                Assert.IsTrue(Math.Abs(eq.Bands[i].Gain - hannResult[i + half]) < .14f);
            }
        }

        /// <summary>
        /// Tests if the Tukey window produces an expected result for a mono signal.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TukeyMono() {
            float[] window = new float[tukeyResult.Length];
            Array.Fill(window, 1);
            Windowing.ApplyWindow(window, Window.Tukey);
            CollectionAssert.AreEqual(window, tukeyResult);
        }

        /// <summary>
        /// Tests if the Tukey window applies to both ends of a signal.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Symmetry() {
            float[] window = new float[10];
            Array.Fill(window, 1);
            Windowing.ApplyWindow(window, Window.Tukey);
            Assert.AreNotEqual(window[0], 1);
            Assert.AreNotEqual(window[^1], 1);
        }

        /// <summary>
        /// A correct example of the Hann window's bounding area.
        /// </summary>
        static readonly float[] hannResult = {
            0, 0.0954915f, 0.34549153f, 0.65450853f, 0.90450853f, 1, 0.9045085f, 0.65450853f, 0.34549144f, 0.09549138f
        };

        /// <summary>
        /// A correct example of the Tukey window's bounding area.
        /// </summary>
        static readonly float[] tukeyResult = {
            0, 0.9045085f, 1, 1, 1, 1, 1, 1, 1, 0.904508233f
        };
    }
}