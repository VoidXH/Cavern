using Cavern.Utilities;
using Cavern.Utilities.Threading;

using Test.Cavern.Consts;

namespace Test.Cavern;

/// <summary>
/// Tests the <see cref="Measurements"/> class.
/// </summary>
[TestClass]
public class Measurements_Tests {
    /// <summary>
    /// Tests if an FFT produces the expected result and the IFFT inverts it correctly for a Dirac-delta.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT_IFFT_Dirac() => CavernAmpTest.RunPlusMono(() => {
        Complex[] signal = new Complex[16];
        signal[0] = new Complex(1, 0);

        using FFTCache cache = new FFTCache(signal.Length);
        signal.InPlaceFFT(cache);
        CheckIfDirac(signal);

        signal.InPlaceIFFT(cache);
        Assert.IsTrue(signal[0].Real == 1);
        Assert.IsTrue(signal[0].Imaginary == 0);
        for (int i = 1; i < signal.Length; i++) {
            Assert.IsTrue(signal[i].Real == 0);
            Assert.IsTrue(signal[i].Imaginary == 0);
        }
    });

    /// <summary>
    /// Tests if an FFT produces the expected result and the IFFT inverts it correctly for a sine wave.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT_IFFT_Sine() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = Generators.Sine(32);

        using FFTCache cache = new FFTCache(signal.Length);
        Complex[] fft = signal.FFT(cache);
        for (int i = 0; i < fft.Length; i++) {
            Assert.IsTrue(i == 1 || i == 31 ? fft[i].Real == (signal.Length >> 1) : Math.Abs(fft[i].Real) < 2 * Constants.delta);
            Assert.IsTrue(Math.Abs(fft[i].Imaginary) < 4 * Constants.delta);
        }

        fft.InPlaceIFFT(cache);
        for (int i = 0; i < fft.Length; i++) {
            Assert.IsTrue(Math.Abs(fft[i].Real - signal[i]) < 2 * Constants.delta);
            Assert.IsTrue(Math.Abs(fft[i].Imaginary) < 2 * Constants.delta);
        }
    });

    /// <summary>
    /// Tests the FFT variant which optimizes the first step of the FFT to keep an absolute result.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT1D() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = Generators.DiracDelta(16);
        float[] fft = signal.FFT1D();
        CheckIfDirac(fft);
    });

    /// <summary>
    /// Tests an FFT with a length of 4, which is calling a hardcoded FFT variant.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT4() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = [1, .5f, .25f, 0];
        Complex[] fft = signal.FFT();
        Complex[] expected = [new Complex(1.75f), new Complex(.75f, -.5f), new Complex(.75f), new Complex(.75f, .5f)];
        CollectionAssert.AreEqual(expected, fft);
    });

    /// <summary>
    /// Tests an FFT with a length of 8, which is calling a hardcoded FFT variant.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT8() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = [1, 2, 3, 4, 0, 0, 0, 0];
        Complex[] fft = signal.FFT();
        Complex[] expected = [new Complex(10), new Complex(-0.41421354f, -7.2426405f), new Complex(-2f, 2f),
            new Complex(2.4142137f, -1.2426405f), new Complex(-2f, 0f),
            new Complex(2.4142137f, 1.2426405f), new Complex(-2f, -2f), new Complex(-0.41421354f, 7.2426405f)];
        CollectionAssert.AreEqual(expected, fft);
    });

    /// <summary>
    /// Tests an FFT with a length of 1024, which is calling the recursive FFT variant.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT1024() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = Generators.DiracDelta(1024);
        Complex[] fft = signal.FFT();
        CheckIfDirac(fft);
    });

    /// <summary>
    /// Tests an FFT with a large length, while another threads are also running same-sized FFTs with random input values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT_Multithreaded() => CavernAmpTest.RunPlusMono(() => {
        float[][] signals = [
            Generators.DiracDelta(4096),
            Generators.Random(4096),
            Generators.Random(4096),
            Generators.Random(4096)
        ];
        Complex[][] results = new Complex[4][];
        Parallelizer.For(0, 4, i => {
            results[i] = signals[i].FFT();
        });
        CheckIfDirac(results[0]);
    });

    /// <summary>
    /// Tests if a random FFT using the same FFT cache runs correctly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FFT_Reuse() => CavernAmpTest.RunPlusMono(() => {
        float[] signal = Generators.Random(1024);
        ThreadSafeFFTCache cache = new(1024);
        signal.InPlaceFFT(cache);
        signal.Clear();
        signal[0] = 1;
        signal.InPlaceFFT(cache);
        CheckIfDirac(signal);
    });

    /// <summary>
    /// Tests the <see cref="Measurements.GetRealPart(Complex[], float[])"/> method.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetRealPart() {
        float[] realPart = new float[Constants.complexSamples.Length];
        Constants.complexSamples.GetRealPart(realPart);
        CollectionAssert.AreEqual(Constants.samples, realPart);
    }

    /// <summary>
    /// Check if an <paramref name="fft"/> result is a Dirac-delta.
    /// </summary>
    static void CheckIfDirac(Complex[] fft) {
        for (int i = 0; i < fft.Length; i++) {
            if (fft[i].Real != 1 || fft[i].Imaginary != 0) {
                Assert.Fail($"Non-unit value found at index {i}: {fft[i]}.");
            }
        }
    }

    /// <summary>
    /// Check if an in-place <paramref name="fft"/> result is a Dirac-delta.
    /// </summary>
    static void CheckIfDirac(float[] fft) {
        for (int i = 0; i < fft.Length; i++) {
            if (fft[i] != 1) {
                Assert.Fail($"Non-1 value found at index {i}: {fft[i]}.");
            }
        }
    }
}
