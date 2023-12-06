using Cavern.QuickEQ.EQCurves;

namespace Test.Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Tests the <see cref="RoomCurve"/> class.
    /// </summary>
    [TestClass]
    public class RoomCurve_Tests {
        /// <summary>
        /// Tests if <see cref="RoomCurve"/>'s this operator works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void This() => EQCurveTestUtils.RoomCurveLikeThis(new RoomCurve(), 200, 3, 3);

        /// <summary>
        /// Tests if <see cref="RoomCurve.GenerateLinearCurve(int, int, float)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GenerateLinearCurve() => EQCurveTestUtils.RoomCurveLikeGenerateLinearCurve(new RoomCurve(), 200, 3, 3);
    }
}