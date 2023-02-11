using Cavern.QuickEQ;

namespace Test.Cavern {
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
    }
}