using Cavern.Filters;
using Cavern.Utilities;

namespace Test.Cavern.Filters {
    /// <summary>
    /// Tests the <see cref="Convolver"/> and <see cref="FastConvolver"/> classes with their descendants too.
    /// </summary>
    [TestClass]
    public class Convolver_Tests {
        /// <summary>
        /// Tests if <see cref="FastConvolver"/> works correctly for a mono signal.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FastConvolverMono() {
            float[] dirac = new float[] { 1, 0, 0, 0 };
            float[] step = new float[] { 1, .75f, .5f, 2 };
            new FastConvolver(step).Process(dirac);
            CollectionAssert.AreEqual(step, dirac);
        }

        /// <summary>
        /// Tests if <see cref="FastConvolver"/> works correctly for a stereo signal's single channel.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FastConvolverStereo() {
            float[] dirac = new float[] { 1, .5f, 0, 1, 0, 1, 0, .5f };
            float[] step = new float[] { 1, .75f, .5f, 2 };
            new FastConvolver(step).Process(dirac, 0, 2);

            float[] left = new float[step.Length];
            WaveformUtils.ExtractChannel(dirac, left, 0, 2);
            CollectionAssert.AreEqual(step, left);
        }

        /// <summary>
        /// Tests the <see cref="FastConvolver.ConvolveSafe(float[], float[])"/> method.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FastConvolverSafe() {
            float[] result = FastConvolver.ConvolveSafe(Consts.samples, Consts.samples2);
            for (int i = 0; i < result.Length; i++) {
                Assert.AreEqual(Consts.convolved[i], result[i], Consts.delta);
            }
        }
    }
}