using Cavern.QuickEQ.EQCurves;

namespace Test.Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Tests the <see cref="EQCurve"/> class.
    /// </summary>
    [TestClass]
    public class EQCurve_Tests {
        /// <summary>
        /// Tests if <see cref="EQCurve.GetAverageLevel(double, double, double)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetAverageLevel() {
            EQCurve curve = new Punch(3);
            Assert.AreEqual(1.8299298951978702, curve.GetAverageLevel(20, 120, 10), Consts.delta);
            Assert.AreEqual(0, curve.GetAverageLevel(200, 1000, 100), Consts.delta);
            Assert.AreEqual(-0.34931992656327837, curve.GetAverageLevel(1000, 2000, 100), Consts.delta);
        }
    }
}