using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="Measurements"/> class.
    /// </summary>
    [TestClass]
    public class Measurements_Tests {
        /// <summary>
        /// Tests if an FFT produces the expected result and the IFFT inverts it correctly for a Dirac-delta.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT_IFFT_Dirac() {
            Complex[] signal = new Complex[16];
            signal[0] = new Complex(1, 0);

            using FFTCache cache = new FFTCache(signal.Length);
            signal.InPlaceFFT(cache);
            for (int i = 0; i < signal.Length; i++) {
                Assert.IsTrue(signal[i].Real == 1);
                Assert.IsTrue(signal[i].Imaginary == 0);
            }

            signal.InPlaceIFFT(cache);
            Assert.IsTrue(signal[0].Real == 1);
            Assert.IsTrue(signal[0].Imaginary == 0);
            for (int i = 1; i < signal.Length; i++) {
                Assert.IsTrue(signal[i].Real == 0);
                Assert.IsTrue(signal[i].Imaginary == 0);
            }
        }

        /// <summary>
        /// Tests if an FFT produces the expected result and the IFFT inverts it correctly for a sine wave.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT_IFFT_Sine() {
            float[] signal = new float[32];
            for (int i = 0; i < signal.Length; i++) {
                signal[i] = MathF.Cos(2 * MathF.PI * i / signal.Length);
            }

            using FFTCache cache = new FFTCache(signal.Length);
            Complex[] fft = signal.FFT(cache);
            for (int i = 0; i < fft.Length; i++) {
                Assert.IsTrue(i == 1 || i == 31 ? fft[i].Real == (signal.Length >> 1) : Math.Abs(fft[i].Real) < 2 * Consts.delta);
                Assert.IsTrue(Math.Abs(fft[i].Imaginary) < 4 * Consts.delta);
            }

            fft.InPlaceIFFT(cache);
            for (int i = 0; i < fft.Length; i++) {
                Assert.IsTrue(Math.Abs(fft[i].Real - signal[i]) < 2 * Consts.delta);
                Assert.IsTrue(Math.Abs(fft[i].Imaginary) < 2 * Consts.delta);
            }
        }

        /// <summary>
        /// Tests the FFT variant which optimizes the first step of the FFT to keep an absolute result.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT1D() {
            float[] signal = new float[16];
            signal[0] = 1;
            float[] fft = signal.FFT1D();
            for (int i = 0; i < fft.Length; i++) {
                Assert.IsTrue(fft[i] == 1);
            }
        }

        /// <summary>
        /// Tests an FFT with a length of 4, which is calling a hardcoded FFT variant.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT4() {
            float[] signal = [1, .5f, .25f, 0];
            Complex[] fft = signal.FFT();
            Complex[] expected = [new Complex(1.75f), new Complex(.75f, -.5f), new Complex(.75f), new Complex(.75f, .5f)];
            CollectionAssert.AreEqual(expected, fft);
        }

        /// <summary>
        /// Tests an FFT with a length of 4, which is calling a hardcoded FFT variant.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void FFT8() {
            float[] signal = [1, 2, 3, 4, 0, 0, 0, 0];
            Complex[] fft = signal.FFT();
            Complex[] expected = [new Complex(10), new Complex(-0.41421354f, -7.2426405f), new Complex(-2f, 2f),
                new Complex(2.4142137f, -1.2426405f), new Complex(-2f, 0f),
                new Complex(2.4142137f, 1.2426405f), new Complex(-2f, -2f), new Complex(-0.41421354f, 7.2426405f)];
            CollectionAssert.AreEqual(expected, fft);
        }

        /// <summary>
        /// Tests the <see cref="Measurements.GetRealPart(Complex[], float[])"/> method.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetRealPart() {
            float[] realPart = new float[Consts.complexSamples.Length];
            Consts.complexSamples.GetRealPart(realPart);
            CollectionAssert.AreEqual(Consts.samples, realPart);
        }
    }
}