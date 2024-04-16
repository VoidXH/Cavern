using Cavern.Filters;
using Cavern.QuickEQ.Equalization;

namespace Test.Cavern.QuickEQ.Equalization {
    /// <summary>
    /// Tests the <see cref="PeakingEqualizer"/> class.
    /// </summary>
    [TestClass]
    public class PeakingEqualizer_Tests {
        /// <summary>
        /// Tests if <see cref="PeakingEqualizer.ParseEQFile(string)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ParseEQFile() {
            PeakingEQ[] result = PeakingEqualizer.ParseEQFile(testEQFile);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(20, result[0].CenterFreq);
            Assert.AreEqual(-2, result[0].Gain);
            Assert.AreEqual(12.5, result[0].Q);
            Assert.AreEqual(20.42, result[1].CenterFreq);
            Assert.AreEqual(1, result[1].Gain);
            Assert.AreEqual(10, result[1].Q);
        }

        static readonly string[] testEQFile = [
            "Equaliser: Generic",
            "Filter  1: ON  PK       Fc   20.00 Hz  Gain  -2.00 dB  Q  12.50",
            "Filter  2: ON  PK       Fc   20.42 Hz  Gain   1.00 dB  Q  10.00"
        ];
    }
}