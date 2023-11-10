using Cavern.Filters.Utilities;

namespace Test.Cavern.Filters {
    /// <summary>
    /// Tests the <see cref="QFactor"/> class.
    /// </summary>
    [TestClass]
    public class QFactor_Tests {
        /// <summary>
        /// Tests if conversions work correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TrailingZeros() {
            Assert.AreEqual(0.1442094593213907, QFactor.ToBandwidth(10), Consts.delta);
            Assert.AreEqual(10, QFactor.FromBandwidth(0.1442094593213907), Consts.delta);
        }
    }
}