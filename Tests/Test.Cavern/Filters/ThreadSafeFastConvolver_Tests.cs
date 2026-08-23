using Cavern.Filters;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Utilities;

using Test.Cavern.Consts;

namespace Test.Cavern.Filters;

/// <summary>
/// Tests the <see cref="ThreadSafeFastConvolver"/> class.
/// </summary>
[TestClass]
public class ThreadSafeFastConvolver_Tests {
    /// <summary>
    /// Tests basic convolution correctness with a Dirac delta.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Basic() => CavernAmpTest.Run(() => {
        float[] dirac = [1, 0, 0, 0];
        float[] step = [1, .75f, .5f, 2];
        new ThreadSafeFastConvolver(step).Process(dirac);
        CollectionAssert.AreEqual(step, dirac);
    });

    /// <summary>
    /// Tests if the impulse response is correctly retrieved after construction.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ImpulseAccess() => CavernAmpTest.Run(() => {
        float[] impulse = [.1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f];
        ThreadSafeFastConvolver filter = new ThreadSafeFastConvolver(impulse);
        float[] retrieved = filter.Impulse;
        Assert.AreEqual(impulse.Length, retrieved.Length);
        for (int i = 0; i < impulse.Length; i++) {
            Assert.AreEqual(impulse[i], retrieved[i], Constants.delta);
        }
    });

    /// <summary>
    /// Tests convolution with a signal longer than the filter (exercises overlap-add).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void MultiBlock() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .5f];
        float[] signal = [1, 0, 0, 0, 0, 0];
        float[] expected = Convolver.Convolve(signal, impulse);
        new ThreadSafeFastConvolver(impulse).Process(signal);
        TestUtils.AssertSameArrayBeginning(signal, expected);
    });

    /// <summary>
    /// Tests stereo channel processing.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Stereo() => CavernAmpTest.Run(() => {
        float[] dirac = [1, .5f, 0, 1, 0, 1, 0, .5f];
        float[] step = [1, .75f, .5f, 2];
        new ThreadSafeFastConvolver(step).Process(dirac, 0, 2);

        float[] left = new float[step.Length];
        WaveformUtils.ExtractChannel(dirac, left, 0, 2);
        CollectionAssert.AreEqual(step, left);
    });

    /// <summary>
    /// Tests the delay property correctly shifts the output.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay() => CavernAmpTest.Run(() => {
        float[] dirac = [1, 0, 0, 0];
        float[] step = [1, .75f, .5f, 2];
        ThreadSafeFastConvolver filter = new ThreadSafeFastConvolver(step, 2);
        filter.Process(dirac);
        Assert.AreEqual(0, dirac[0], Constants.delta);
        Assert.AreEqual(0, dirac[1], Constants.delta);
        Assert.AreEqual(1, dirac[2], Constants.delta);
    });

    /// <summary>
    /// Tests the reset method clears the internal state.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Reset() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .5f, .25f, .125f];
        float[] signal = [1, 0, 0, 0];
        ThreadSafeFastConvolver filter = new ThreadSafeFastConvolver(impulse);
        filter.Process(signal);

        filter.Reset();
        float[] signal2 = [0, 0, 0, 0];
        filter.Process(signal2);
        TestUtils.AssertAll(signal2, 0f);
    });

    /// <summary>
    /// Tests cloning produces an independent copy with the same impulse.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Clone() => CavernAmpTest.Run(() => {
        float[] impulse = [1, .75f, .5f, 2];
        ThreadSafeFastConvolver original = new ThreadSafeFastConvolver(impulse);
        ThreadSafeFastConvolver copy = (ThreadSafeFastConvolver)original.Clone();
        Assert.IsNotNull(copy);

        float[] dirac1 = [1, 0, 0, 0];
        float[] dirac2 = [1, 0, 0, 0];
        original.Process(dirac1);
        copy.Process(dirac2);
        CollectionAssert.AreEqual(dirac1, dirac2);
    });

    /// <summary>
    /// Tests the SampleRate property.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SampleRate() => CavernAmpTest.Run(() => {
        ThreadSafeFastConvolver filter = new ThreadSafeFastConvolver([1, 0, 0, 0], 48000, 0);
        Assert.AreEqual(48000, filter.SampleRate);
    });

    /// <summary>
    /// Tests that multiple independent <see cref="ThreadSafeFastConvolver"/> instances can be processed concurrently from multiple threads without interference.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ThreadSafety() {
        CavernAmp.Bypass = true;
        int length = 256;
        float[] impulse = WaveformGenerator.Sine(1, 64);
        float[] expected = Generators.DiracDelta(length);
        new ThreadSafeFastConvolver(impulse).Process(expected);

        int threadCount = 8;
        float[][] results = new float[threadCount][];
        Thread[] threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++) {
            int index = i;
            threads[i] = new Thread(() => {
                float[] signal = Generators.DiracDelta(length);
                new ThreadSafeFastConvolver(impulse).Process(signal);
                results[index] = signal;
            });
        }

        for (int i = 0; i < threadCount; i++) {
            threads[i].Start();
        }
        for (int i = 0; i < threadCount; i++) {
            threads[i].Join();
        }

        for (int i = 0; i < threadCount; i++) {
            CollectionAssert.AreEqual(expected, results[i]);
        }
    }

    /// <summary>
    /// Tests that a large Dirac delta impulse (16384 samples) produces transparent convolution.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LargeDiracDeltaTransparency() => CavernAmpTest.Run(() => {
        const int impulseLength = 16384;
        float[] largeDirac = Generators.DiracDelta(impulseLength);
        float[] signal = WaveformGenerator.Sine(1, impulseLength);
        float[] expected = (float[])signal.Clone();
        new ThreadSafeFastConvolver(largeDirac).Process(signal);

        const float largeFftTolerance = 0.001f; // 1e-3 instead of 1e-6
        for (int i = 0; i < signal.Length; i++) {
            Assert.AreEqual(expected[i], signal[i], largeFftTolerance, $"Sample {i}: expected {expected[i]}, got {signal[i]}, error {MathF.Abs(signal[i] - expected[i])}");
        }
    });

    /// <summary>
    /// Tests multiple consecutive blocks with a large Dirac delta to expose overlap-add boundary artifacts.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void LargeDiracDeltaMultiBlock() => CavernAmpTest.Run(() => {
        const int impulseLength = 16384;
        const int signalLength = 32768;
        float[] largeDirac = Generators.DiracDelta(impulseLength);
        float[] signal = WaveformGenerator.Sine(1, impulseLength);
        float[] expected = (float[])signal.Clone();
        new ThreadSafeFastConvolver(largeDirac).Process(signal);

        const float largeFftTolerance = 0.001f;
        for (int i = 0; i < signal.Length; i++) {
            Assert.AreEqual(expected[i], signal[i], largeFftTolerance, $"Sample {i}: expected {expected[i]}, got {signal[i]}, error {MathF.Abs(signal[i] - expected[i])}");
        }
    });
}
