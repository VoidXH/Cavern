using Cavern.Utilities;

using Test.Cavern.Consts;

namespace Test.Cavern.Utilities;

/// <summary>
/// Tests the subsample delay functionality of <see cref="WaveformUtils.Delay(float[], float)"/>.
/// </summary>
[TestClass]
public class WaveformUtils_SubsampleDelay_Tests {
    /// <summary>
    /// Finds the index of the peak magnitude in the given waveform.
    /// </summary>
    /// <param name="waveform">The waveform to analyze.</param>
    /// <returns>The index of the sample with the highest absolute value.</returns>
    static int FindPeakIndex(float[] waveform) {
        int peakIndex = 0;
        for (int i = 1; i < waveform.Length; i++) {
            if (Math.Abs(waveform[i]) > Math.Abs(waveform[peakIndex])) {
                peakIndex = i;
            }
        }
        return peakIndex;
    }

    /// <summary>
    /// Runs a delay test on a Dirac delta impulse and asserts the peak index is exact.
    /// </summary>
    /// <param name="bufferSize">The size of the Dirac delta buffer.</param>
    /// <param name="delay">The delay in samples.</param>
    /// <param name="expectedPeakIndex">The exact expected peak index.</param>
    static void AssertPeakIndexExact(int bufferSize, float delay, int expectedPeakIndex) => CavernAmpTest.Run(() => {
        float[] impulse = Generators.DiracDelta(bufferSize);
        WaveformUtils.Delay(impulse, delay);

        int peakIndex = FindPeakIndex(impulse);
        Assert.AreEqual(expectedPeakIndex, peakIndex, $"Peak should be at sample index {expectedPeakIndex} after delaying by {delay} samples.");
    });

    /// <summary>
    /// Runs a delay test on a Dirac delta impulse and asserts the peak index is within a range.
    /// </summary>
    /// <param name="bufferSize">The size of the Dirac delta buffer.</param>
    /// <param name="delay">The delay in samples.</param>
    /// <param name="expectedPeaks">The acceptable peak indices (typically 2 adjacent indices for fractional delays).</param>
    static void AssertPeakIndexInRange(int bufferSize, float delay, int[] expectedPeaks) => CavernAmpTest.Run(() => {
        float[] impulse = Generators.DiracDelta(bufferSize);
        WaveformUtils.Delay(impulse, delay);

        int peakIndex = FindPeakIndex(impulse);
        Assert.IsTrue(
            expectedPeaks.Contains(peakIndex),
            $"Peak should be near index {string.Join(" or ", expectedPeaks)} for {delay} sample delay, but was at index {peakIndex}.");
    });

    /// <summary>
    /// Runs a delay test on a Dirac delta impulse and asserts the peak magnitude exceeds a threshold.
    /// </summary>
    /// <param name="bufferSize">The size of the Dirac delta buffer.</param>
    /// <param name="delay">The delay in samples.</param>
    /// <param name="minMagnitude">The minimum acceptable peak magnitude.</param>
    private static void AssertPeakMagnitude(int bufferSize, float delay, float minMagnitude) => CavernAmpTest.Run(() => {
        float[] impulse = Generators.DiracDelta(bufferSize);
        WaveformUtils.Delay(impulse, delay);

        float peak = impulse.GetPeak();
        Assert.IsTrue(
            peak > minMagnitude,
            $"Peak magnitude should be > {minMagnitude} for {(delay == (int)delay ? "integer" : "fractional")} delay on {bufferSize}-sample buffer, but was {peak}.");
    });

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 5 samples places the peak at index 5.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_5() => AssertPeakIndexExact(32, 5, 5);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 0 samples keeps the peak at index 0.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_0() => AssertPeakIndexExact(32, 0, 0);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 10 samples places the peak at index 10.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_10() => AssertPeakIndexExact(32, 10, 10);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 1 sample places the peak at index 1.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_1() => AssertPeakIndexExact(32, 1, 1);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by a fractional amount (5.5 samples)
    /// produces a peak near index 5 or 6 (interpolated between samples).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_5_5() => AssertPeakIndexInRange(32, 5.5f, [5, 6]);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 3.25 samples places the peak near index 3.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_3_25() => AssertPeakIndexInRange(32, 3.25f, [3, 4]);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 15 samples places the peak at index 15.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_15() => AssertPeakIndexExact(32, 15, 15);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 31 samples places the peak at index 31.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_31() => AssertPeakIndexExact(32, 31, 31);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 16 samples places the peak at index 16.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_16() => AssertPeakIndexExact(32, 16, 16);

    /// <summary>
    /// Tests if the peak magnitude stays reasonable for integer delays on 32-sample buffers.
    /// Small buffers have limited FFT resolution, so magnitude drops are expected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Peak_Magnitude_Integer_Delay() => AssertPeakMagnitude(32, 5, .9f);

    /// <summary>
    /// Tests if the peak magnitude stays reasonable for fractional delays on 32-sample buffers.
    /// Small buffers have limited FFT resolution, so magnitude drops are expected.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Peak_Magnitude_Fractional_Delay() => AssertPeakMagnitude(32, 5.5f, .6f);

    /// <summary>
    /// Tests if the peak magnitude stays reasonable for integer delays on 128-sample buffers.
    /// FFT-based subsample delay introduces spectral leakage, so magnitude is not perfectly preserved.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_128_Sample_Dirac_Delta_Peak_Magnitude_Integer_Delay() => AssertPeakMagnitude(32, 5, .9f);

    /// <summary>
    /// Tests if the peak magnitude stays reasonable for fractional delays on 128-sample buffers.
    /// FFT-based subsample delay introduces spectral leakage, so magnitude is not perfectly preserved.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_128_Sample_Dirac_Delta_Peak_Magnitude_Fractional_Delay() => AssertPeakMagnitude(32, 5.5f, .6f);

    /// <summary>
    /// Tests if delaying a 64-sample Dirac delta by 5 samples places the peak at index 5.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_64_Sample_Dirac_Delta_Delayed_By_5() => AssertPeakIndexExact(32, 5, 5);

    /// <summary>
    /// Tests if delaying a 128-sample Dirac delta by 5 samples places the peak at index 5.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_128_Sample_Dirac_Delta_Delayed_By_5() => AssertPeakIndexExact(32, 5, 5);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 0.5 samples places the peak near index 0 or 1.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_0_5() => AssertPeakIndexInRange(32, .5f, [0, 1]);

    /// <summary>
    /// Tests if delaying a 32-sample Dirac delta by 7.75 samples places the peak near index 8.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Delay_32_Sample_Dirac_Delta_Delayed_By_7_75() => AssertPeakIndexInRange(32, 7.75f, [7, 8]);
}
