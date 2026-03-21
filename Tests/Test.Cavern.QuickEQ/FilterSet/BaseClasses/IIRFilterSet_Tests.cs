using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.FilterSet;
using Cavern.Utilities;

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
}
