using Cavern.QuickEQ.EQCurves;

namespace Test.Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Tests the <see cref="Depth"/> class.
    /// </summary>
    [TestClass]
    public class Depth_Tests {
        /// <summary>
        /// Tests if <see cref="Depth"/>'s this operator works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void This() => EQCurveTestUtils.RoomCurveLikeIndexer(new Depth(), 60, 12, 3);

        /// <summary>
        /// Tests if <see cref="Depth.GenerateLinearCurve(int, int, float)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GenerateLinearCurve() => EQCurveTestUtils.RoomCurveLikeLinearCurve(new Depth(), 60, 12, 3);

        /// <summary>
        /// Tests if <see cref="Depth.GenerateLogCurve(double, double, int, float)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GenerateLogCurve() => EQCurveTestUtils.RoomCurveLikeLogCurve(new Depth(), 60, 12, 3);
    }
}
