using Cavern.Filters;
using Cavern.QuickEQ.Equalization;

namespace Test.Cavern.QuickEQ {
    /// <summary>
    /// Tests the <see cref="PeakingEqualizer"/> class.
    /// </summary>
    [TestClass]
    public class PeakingEqualizer_Tests {
        /// <summary>
        /// Tests if the private method brute forcing gains works as intended.
        /// </summary>
        [TestMethod, Timeout(10000)]
        public void BruteForceGains() {
            Equalizer slash = new Equalizer();
            slash.AddBand(new Band(20, 0));
            slash.AddBand(new Band(20000, 10));
            PeakingEqualizer peq = new PeakingEqualizer(slash) {
                MinGain = -6,
                MaxGain= 6,
            };
            PeakingEQ[] result = peq.GetPeakingEQ(48000, 31.25, 1, 10);
            for (int i = 1; i < result.Length; i++) {
                Assert.IsTrue(result[i - 1].Gain - .05f /* eps */ < result[i].Gain); // The gains in a slash EQ have to grow
            }
        }
    }
}