using Cavern.Filters.Utilities;

namespace Test.Cavern.Filters {
    /// <summary>
    /// Tests the <see cref="QFactor"/> class.
    /// </summary>
    [TestClass]
    public class QFactor_Tests {
        /// <summary>
        /// Tests if bandwidth conversions work correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Bandwidth() {
            const double inQ = 10,
                inBandwidth = 0.1442094593213907;
            Assert.AreEqual(inBandwidth, QFactor.ToBandwidth(inQ), Consts.delta);
            Assert.AreEqual(inQ, QFactor.FromBandwidth(inBandwidth), Consts.delta);
        }

        /// <summary>
        /// Tests if slope conversions work correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Slope() {
            const double inQ = 10,
                inSlope = 4.9293152597075665,
                gain = 6;
            Assert.AreEqual(inSlope, QFactor.ToSlope(inQ, gain), Consts.delta);
            Assert.AreEqual(inQ, QFactor.FromSlope(inSlope, gain), Consts.delta);
        }
    }
}