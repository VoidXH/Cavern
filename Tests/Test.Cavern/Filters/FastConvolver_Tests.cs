using Cavern.Filters;
using Cavern.Utilities;

using Test.Cavern.Consts;

namespace Test.Cavern.Filters;

/// <summary>
/// Tests the <see cref="FastConvolver"/> class.
/// </summary>
[TestClass]
public class FastConvolver_Tests {
    /// <summary>
    /// Tests if <see cref="FastConvolver"/> works correctly for a mono signal.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Mono() => CavernAmpTest.Run(() => {
        float[] dirac = [1, 0, 0, 0];
        float[] step = [1, .75f, .5f, 2];
        new FastConvolver(step).Process(dirac);
        CollectionAssert.AreEqual(step, dirac);
    });

    /// <summary>
    /// Tests the <see cref="FastConvolver.ConvolveSafe(float[], float[])"/> method.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ConvolveSafe() => CavernAmpTest.Run(() => {
        float[] result = FastConvolver.ConvolveSafe(Constants.samples, Constants.samples2);
        Assert.IsTrue(result.Length > Constants.convolved.Length);
        TestUtils.AssertSameArrayBeginning(result, Constants.convolved);
    });

    /// <summary>
    /// Tests if <see cref="FastConvolver"/> works correctly for a stereo signal's single channel.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stereo() => CavernAmpTest.Run(() => {
        float[] dirac = [1, .5f, 0, 1, 0, 1, 0, .5f];
        float[] step = [1, .75f, .5f, 2];
        new FastConvolver(step).Process(dirac, 0, 2);

        float[] left = new float[step.Length];
        WaveformUtils.ExtractChannel(dirac, left, 0, 2);
        CollectionAssert.AreEqual(step, left);
    });

    /// <summary>
    /// Tests convolution with a signal longer than the filter (overlap-add across multiple blocks).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void MultiBlock() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        float[] signal = [1, 0, 0, 0, 0, 0, 0, 0];
        float[] expected = Convolver.Convolve(signal, impulse);
        new FastConvolver(impulse).Process(signal);
        TestUtils.AssertSameArrayBeginning(signal, expected);
    });

    /// <summary>
    /// Tests the delay property shifts the output by the specified amount.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay() => CavernAmpTest.Run(() => {
        float[] dirac = [1, 0, 0, 0];
        float[] step = [1, .75f, .5f, 2];
        FastConvolver filter = new FastConvolver(step, 2);
        filter.Process(dirac);
        Assert.AreEqual(0, dirac[0], Constants.delta);
        Assert.AreEqual(0, dirac[1], Constants.delta);
        Assert.AreEqual(1, dirac[2], Constants.delta);
        Assert.AreEqual(.75f, dirac[3], Constants.delta);
    });

    /// <summary>
    /// Tests the reset method clears the future buffer.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Reset() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .5f, .25f, .125f];
        float[] signal = [1, 0, 0, 0];
        FastConvolver filter = new FastConvolver(impulse);
        filter.Process(signal);

        filter.Reset();
        float[] silence = [0, 0, 0, 0];
        filter.Process(silence);
        TestUtils.AssertAll(silence, 0f);
    });

    /// <summary>
    /// Tests cloning produces an independent copy.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Clone() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        FastConvolver original = new(impulse);
        FastConvolver copy = (FastConvolver)original.Clone();

        float[] dirac1 = [1, 0, 0, 0];
        float[] dirac2 = [1, 0, 0, 0];
        original.Process(dirac1);
        copy.Process(dirac2);
        CollectionAssert.AreEqual(dirac1, dirac2);
    });

    /// <summary>
    /// Tests that cloning preserves the delay.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void CloneWithDelay() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        FastConvolver original = new(impulse, 3);
        FastConvolver copy = (FastConvolver)original.Clone();

        float[] dirac1 = [1, 0, 0, 0, 0, 0, 0];
        float[] dirac2 = [1, 0, 0, 0, 0, 0, 0];
        original.Process(dirac1);
        copy.Process(dirac2);
        CollectionAssert.AreEqual(dirac1, dirac2);
    });

    /// <summary>
    /// Tests the Fourier-space constructor produces correct results.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void FourierConstructor() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        int fftSize = 2 << QMath.Log2Ceil(impulse.Length);
        Complex[] fourierFilter = new Complex[fftSize];
        impulse.ParseForFFT(fourierFilter);
        using FFTCache cache = new ThreadSafeFFTCache(fftSize);
        fourierFilter.InPlaceFFT(cache);

        FastConvolver filter = new(fourierFilter);
        float[] dirac = [1, 0, 0, 0];
        filter.Process(dirac);
        for (int i = 0; i < impulse.Length; i++) {
            Assert.AreEqual(impulse[i], dirac[i], Constants.delta);
        }
    });

    /// <summary>
    /// Tests the SampleRate property.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SampleRate() => CavernAmpTest.Run(() => {
        FastConvolver filter = new([1, 0, 0, 0], 48000, 0);
        Assert.AreEqual(48000, filter.SampleRate);
    });

    /// <summary>
    /// Power-of-two sample arrays for static method tests that require them.
    /// </summary>
    static readonly float[] pow2Samples = [.1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f];
    /// <summary>
    /// Power-of-two impulse arrays for static method tests that require them.
    /// </summary>
    static readonly float[] pow2Samples2 = [.6f, .7f, .8f, .9f, 1, 1.1f, 1.2f, 1.3f];
    /// <summary>
    /// Circular convolution of <see cref="pow2Samples"/> with <see cref="pow2Samples2"/> (FFT convolution wraps around).
    /// </summary>
    static readonly float[] pow2Convolved = [3.28f, 3.48f, 3.6f, 3.64f, 3.6f, 3.48f, 3.28f, 3.0f];

    /// <summary>
    /// Tests <see cref="FastConvolver.Convolve(float[], float[])"/> (static, without cache).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StaticConvolve() => CavernAmpTest.Run(() => {
        float[] result = FastConvolver.Convolve(pow2Samples, pow2Samples2);
        TestUtils.AssertSameArrayBeginning(result, pow2Convolved);
    });

    /// <summary>
    /// Tests <see cref="FastConvolver.Convolve(float[], float[], FFTCache)"/> (static, with cache).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StaticConvolveWithCache() => CavernAmpTest.Run(() => {
        using FFTCache cache = new ThreadSafeFFTCache(pow2Samples.Length);
        float[] result = FastConvolver.Convolve(pow2Samples, pow2Samples2, cache);
        TestUtils.AssertSameArrayBeginning(result, pow2Convolved);
    });

    /// <summary>
    /// Tests <see cref="FastConvolver.ConvolveFourier(float[], float[])"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ConvolveFourier() => CavernAmpTest.Run(() => {
        Complex[] result = FastConvolver.ConvolveFourier(pow2Samples, pow2Samples2);
        result.InPlaceIFFT();
        float[] real = Measurements.GetRealPart(result);
        TestUtils.AssertSameArrayBeginning(real, pow2Convolved);
    });

    /// <summary>
    /// Tests <see cref="FastConvolver.ConvolveFourier(float[], float[], FFTCache)"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ConvolveFourierWithCache() => CavernAmpTest.Run(() => {
        using FFTCache cache = new ThreadSafeFFTCache(pow2Samples.Length);
        Complex[] result = FastConvolver.ConvolveFourier(pow2Samples, pow2Samples2, cache);
        result.InPlaceIFFT();
        float[] real = Measurements.GetRealPart(result);
        TestUtils.AssertSameArrayBeginning(real, pow2Convolved);
    });

    /// <summary>
    /// Tests <see cref="FastConvolver.ConvolveSafe(float[], float[], FFTCache)"/>.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ConvolveSafeWithCache() => CavernAmpTest.Run(() => {
        int cacheSize = 1 << QMath.Log2Ceil(Constants.samples.Length + Constants.samples2.Length);
        using FFTCache cache = new ThreadSafeFFTCache(cacheSize);
        float[] result = FastConvolver.ConvolveSafe(Constants.samples, Constants.samples2, cache);
        Assert.IsTrue(result.Length >= Constants.convolved.Length);
        TestUtils.AssertSameArrayBeginning(result, Constants.convolved);
    });

    /// <summary>
    /// Tests that Dispose frees resources and doesn't throw on reuse.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Dispose() => CavernAmpTest.Run(() => {
        FastConvolver filter = new([1, .75f, .5f, 2]);
        filter.Dispose();
        filter.Dispose();
    });

    /// <summary>
    /// Tests the default ToString returns "Convolution".
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ToStringOverride() {
        FastConvolver filter = new([1, .75f, .5f, 2]);
        Assert.AreEqual("Convolution", filter.ToString());
    }

    /// <summary>
    /// Tests impulse change via the setter on an existing filter.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ImpulseSetter() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        FastConvolver filter = new(impulse);
        float[] dirac1 = [1, 0, 0, 0];
        filter.Process(dirac1);
        TestUtils.AssertSameArrayBeginning(impulse, dirac1);

        float[] impulse2 = [2, 4, 6, 8];
        filter.Impulse = impulse2;
        float[] dirac2 = [1, 0, 0, 0];
        filter.Process(dirac2);
        TestUtils.AssertSameArrayBeginning(impulse2, dirac2);
    });
}
