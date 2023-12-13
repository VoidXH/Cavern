using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.FilterSet;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ.FilterSet.BaseClasses {
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
            IIRFilterSet testSet = new IIRFilterSet(1, Consts.sampleRate);
            Lowpass filter = new Lowpass(Consts.sampleRate, Consts.sampleRate >> 2, QFactor.reference);
            testSet.SetupChannel(0, [filter]);
            MultichannelWaveform fir = testSet.GetConvolutionFilter(Consts.sampleRate, Consts.convolutionLength);
            fir[0].InPlaceFFT();
            TestUtils.AssertDecrease(fir[0], 0, Consts.convolutionLength / 2, Consts.delta);
            Assert.AreEqual(fir[0][Consts.convolutionLength / 4], QFactor.reference, Consts.delta);
        }
    }
}
