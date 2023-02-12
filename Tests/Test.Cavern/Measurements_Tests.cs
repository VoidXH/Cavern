using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="Measurements"/> class.
    /// </summary>
    [TestClass]
    public class Measurements_Tests {
        /// <summary>
        /// Tests if an FFT produces the expected result and the IFFT inverts it correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT_IFFT() {
            Complex[] signal = new Complex[16];
            signal[0] = new Complex(1, 0);
            Measurements.InPlaceFFT(signal);
            for (int i = 0; i < signal.Length; i++) {
                Assert.IsTrue(signal[i].Real == 1);
                Assert.IsTrue(signal[i].Imaginary == 0);
            }
            Measurements.InPlaceIFFT(signal);
            Assert.IsTrue(signal[0].Real == 1);
            Assert.IsTrue(signal[0].Imaginary == 0);
            for (int i = 1; i < signal.Length; i++) {
                Assert.IsTrue(signal[i].Real == 0);
                Assert.IsTrue(signal[i].Imaginary == 0);
            }
        }
    }
}