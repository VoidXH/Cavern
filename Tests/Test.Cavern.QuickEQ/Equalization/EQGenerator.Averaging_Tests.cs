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
                avg = EQGenerator.AverageRMS(a, b);
            Assert.AreEqual(4.494369506972679, avg.Bands[0].Gain);
        }
    }
}