using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.FilterSet;
using Cavern.Utilities;
using Cavern.Waveforms;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.FilterSet.BaseClasses;

/// <summary>
/// Tests the <see cref="IIRFilterSet"/> class.
/// </summary>
[TestClass]
public class IIRFilterSet_Tests {
    /// <summary>
    /// Tests if the simulation of a filter set results in a correct spectrum by applying a lowpass at half the Nyquist frequency.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Simulation() {
        IIRFilterSet testSet = new IIRFilterSet(1, Constants.sampleRate);
        Lowpass filter = new Lowpass(Constants.sampleRate, Constants.sampleRate >> 2, QFactor.reference);
        testSet.SetupChannel(0, [filter]);
        MultichannelWaveform fir = testSet.GetConvolutionFilter(Constants.sampleRate, Constants.convolutionLength);
        fir[0].InPlaceFFT();
        TestUtils.AssertDecrease(fir[0], 0, Constants.convolutionLength / 2, Constants.delta);
        Assert.AreEqual(fir[0][Constants.convolutionLength / 4], QFactor.reference, Constants.delta);
    }

    /// <summary>
    /// Tests if a filter set constructed from its own <see cref="IIRFilterSet.Export()"/> output recovers the same filters.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void ExportRoundTrip() {
        IIRFilterSet source = new IIRFilterSet(2, Constants.sampleRate);
        PeakingEQ[] left = [
            new PeakingEQ(Constants.sampleRate, 100, 1.5, -3),
            new PeakingEQ(Constants.sampleRate, 1000, 2.5, 4)
        ];
        PeakingEQ[] right = [
            new PeakingEQ(Constants.sampleRate, 200, 0.8, 2)
        ];
        source.SetupChannel(0, left);
        source.SetupChannel(1, right);

        string exported = source.Export();
        IIRFilterSet parsed = new IIRFilterSet(exported, Constants.sampleRate);
        Assert.AreEqual(2, parsed.ChannelCount);

        BiquadFilter[] parsedLeft = ((IIRFilterSet.IIRChannelData)parsed.Channels[0]).filters;
        Assert.AreEqual(left.Length, parsedLeft.Length);
        for (int i = 0; i < left.Length; i++) {
            Assert.AreEqual(left[i].CenterFreq, parsedLeft[i].CenterFreq, Constants.delta);
            Assert.AreEqual(left[i].Gain, parsedLeft[i].Gain, Constants.delta);
            Assert.AreEqual(left[i].Q, parsedLeft[i].Q, Constants.delta);
        }

        BiquadFilter[] parsedRight = ((IIRFilterSet.IIRChannelData)parsed.Channels[1]).filters;
        Assert.AreEqual(right.Length, parsedRight.Length);
        for (int i = 0; i < right.Length; i++) {
            Assert.AreEqual(right[i].CenterFreq, parsedRight[i].CenterFreq, Constants.delta);
            Assert.AreEqual(right[i].Gain, parsedRight[i].Gain, Constants.delta);
            Assert.AreEqual(right[i].Q, parsedRight[i].Q, Constants.delta);
        }
    }
}
