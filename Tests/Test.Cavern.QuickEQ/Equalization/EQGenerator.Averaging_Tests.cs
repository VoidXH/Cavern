using Cavern.QuickEQ.Equalization;

namespace Test.Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Tests the <see cref="EQGenerator"/> class's averaging functions.
    /// </summary>
    [TestClass]
    public class EQGeneratorAveraging_Tests {
        /// <summary>
        /// Tests if <see cref="EQGenerator.AverageRMS(Equalizer[])"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void AverageRMS() {
            Equalizer a = new Equalizer([new Band(20, 1)], true),
                b = new Equalizer([new Band(20, 10)], true),
                avg = EQGenerator.AverageRMS(a, b),
                avg_nodiv = EQGenerator.AverageRMS(a, a);
            Assert.AreEqual(7.50466946361249, avg.PeakGain);
            Assert.AreEqual(a.PeakGain, avg_nodiv.PeakGain, Consts.delta);
        }
    }
}