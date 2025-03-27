using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Test.Cavern {
    /// <summary>
    /// Tests the <see cref="QMath"/> class.
    /// </summary>
    [TestClass]
    public class QMath_Tests {
        /// <summary>
        /// Tests if <see cref="QMath.ToStringLimitDecimals(double, int)"/> works correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void ToStringLimitDecimals() {
            Assert.AreEqual("0", QMath.ToStringLimitDecimals(1E-10, 5));
            Assert.AreEqual("0.2", QMath.ToStringLimitDecimals(0.15, 1));
            Assert.AreEqual("0.707", QMath.ToStringLimitDecimals(QFactor.reference, 3));
        }

        /// <summary>
        /// Tests if <see cref="QMath.TrailingZeros(int)"/> works correctly.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TrailingZeros() {
            Assert.AreEqual(0, QMath.TrailingZeros(int.MaxValue));
            Assert.AreEqual(3, QMath.TrailingZeros(8));
            Assert.AreEqual(4, QMath.TrailingZeros(2064));
        }
    }
}