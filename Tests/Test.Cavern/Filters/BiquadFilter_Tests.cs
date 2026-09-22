using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

using Test.Cavern.Consts;

namespace Test.Cavern.Filters;

/// <summary>
/// Tests the <see cref="BiquadFilter"/> class.
/// </summary>
[TestClass]
public class BiquadFilter_Tests {
    /// <summary>
    /// Tests that <see cref="BiquadFilter.GetTransferFunction(int)"/> returns the correct number of bins.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_Length() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        Complex[] tf = filter.GetTransferFunction(64);
        Assert.AreEqual(64, tf.Length);
    });

    /// <summary>
    /// Tests that a lowpass filter has unity gain at DC (0 Hz).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_Lowpass_DC() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        Complex[] tf = filter.GetTransferFunction(256);
        Assert.IsTrue(tf[0].Magnitude > 0.99f, $"DC gain should be ~1, got {tf[0].Magnitude}");
    });

    /// <summary>
    /// Tests that a highpass filter has near-zero gain at DC (0 Hz).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_Highpass_DC() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Highpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        Complex[] tf = filter.GetTransferFunction(256);
        Assert.IsTrue(tf[0].Magnitude < 0.01f, $"DC gain should be ~0, got {tf[0].Magnitude}");
    });

    /// <summary>
    /// Tests that a peaking EQ with 0 dB gain has unity magnitude at center frequency.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_PeakingEQ_ZeroGain() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new PeakingEQ(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        Complex[] tf = filter.GetTransferFunction(256);
        int bin = (int)(1000 * tf.Length / Listener.DefaultSampleRate);
        Assert.IsTrue(tf[bin].Magnitude > 0.9f, $"0 dB peaking EQ should have ~1 magnitude at center, got {tf[bin].Magnitude}");
    });

    /// <summary>
    /// Tests that <see cref="BiquadFilter.GetTransferFunction(int)"/> throws for non-positive bin count.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_InvalidBins() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => filter.GetTransferFunction(0));
    });

    /// <summary>
    /// Tests that a lowpass filter with very low cutoff has near-unity gain at DC.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetTransferFunction_Lowpass_VeryLowCutoff_DC() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 100, QFactor.reference, 0);
        Complex[] tf = filter.GetTransferFunction(256);
        Assert.IsTrue(tf[0].Magnitude > 0.99f, $"DC gain should be ~1, got {tf[0].Magnitude}");
    });

    /// <summary>
    /// Tests that <see cref="BiquadFilter.GetTransferFunction(int)"/> closely matches the transfer function produced by <see cref="FilterAnalyzer"/> (which convolves
    /// a Dirac delta and FFTs the result).
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void GetTransferFunction_MatchesFilterAnalyzer() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new PeakingEQ(Listener.DefaultSampleRate, 1000, 5, 6);
        int bins = 1024;

        Complex[] directTF = filter.GetTransferFunction(bins);
        FilterAnalyzer analyzer = new FilterAnalyzer(filter, Listener.DefaultSampleRate);
        Complex[] analyzerTF = analyzer.GetFrequencyResponse();

        float[] analyzerMagnitude = Measurements.GetMagnitude(analyzerTF);
        float[] directMagnitude = Measurements.GetMagnitude(directTF);

        int analyzerResolution = analyzerTF.Length;
        for (int i = 0; i < bins; i++) {
            int analyzerBin = i * analyzerResolution / bins;
            float directMag = directMagnitude[i];
            float analyzerMag = analyzerMagnitude[analyzerBin];
            Assert.IsTrue(MathF.Abs(directMag - analyzerMag) < 0.05f, $"Magnitude mismatch at bin {i} (analyzer bin {analyzerBin}): direct={directMag}, analyzer={analyzerMag}");
        }
    });

    /// <summary>
    /// Tests that <see cref="BiquadFilter.GetFrequencyResponse(int)"/> returns the correct number of bins.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMagnitudeResponse_Length() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        float[] mag = filter.GetFrequencyResponse(64);
        Assert.AreEqual(64, mag.Length);
    });

    /// <summary>
    /// Tests that <see cref="BiquadFilter.GetFrequencyResponse(int)"/> matches <see cref="BiquadFilter.GetTransferFunction(int)"/> for magnitude.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMagnitudeResponse_MatchesTransferFunction() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new PeakingEQ(Listener.DefaultSampleRate, 1000, 5, 6);
        int bins = 256;
        Complex[] tf = filter.GetTransferFunction(bins);
        float[] mag = filter.GetFrequencyResponse(bins);
        float[] expectedMag = Measurements.GetMagnitude(tf);
        for (int i = 0; i < bins; i++) {
            Assert.IsTrue(MathF.Abs(mag[i] - expectedMag[i]) < 0.001f, $"Magnitude mismatch at bin {i}: direct={mag[i]}, from TF={expectedMag[i]}");
        }
    });

    /// <summary>
    /// Tests that a lowpass filter has near-unity magnitude at DC (0 Hz).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMagnitudeResponse_Lowpass_DC() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Lowpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        float[] mag = filter.GetFrequencyResponse(256);
        Assert.IsTrue(mag[0] > 0.99f, $"DC magnitude should be ~1, got {mag[0]}");
    });

    /// <summary>
    /// Tests that a highpass filter has near-zero magnitude at DC (0 Hz).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetMagnitudeResponse_Highpass_DC() => CavernAmpTest.Run(() => {
        BiquadFilter filter = new Highpass(Listener.DefaultSampleRate, 1000, QFactor.reference, 0);
        float[] mag = filter.GetFrequencyResponse(256);
        Assert.IsTrue(mag[0] < 0.01f, $"DC magnitude should be ~0, got {mag[0]}");
    });
}
