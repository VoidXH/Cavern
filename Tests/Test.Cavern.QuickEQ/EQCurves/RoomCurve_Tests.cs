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
        public void This() => EQCurveTestUtils.RoomCurveLikeIndexer(new RoomCurve(), 200, 3, 3);

        /// <summary>
        /// Tests if <see cref="RoomCurve.GenerateLinearCurve(int, int, float)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GenerateLinearCurve() => EQCurveTestUtils.RoomCurveLikeLinearCurve(new RoomCurve(), 200, 3, 3);

        /// <summary>
        /// Tests if <see cref="RoomCurve.GenerateLogCurve(double, double, int, float)"/> works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GenerateLogCurve() => EQCurveTestUtils.RoomCurveLikeLogCurve(new RoomCurve(), 200, 3, 3);

        /// <summary>
        /// Tests if Base64 round-trip preserves <see cref="RoomCurveLikeCurve.GenerateLinearCurve(int, int, float)"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Base64RoundTripPreservesLinearCurve() {
            RoomCurveLikeCurve original = new RoomCurveLikeCurve(150, 5, 25);
            string base64 = original.ToBase64();
            RoomCurveLikeCurve restored = new RoomCurveLikeCurve(200, 3);
            restored.FromBase64(base64);

            float[] originalCurve = original.GenerateLinearCurve(48000, 24000, 0);
            float[] restoredCurve = restored.GenerateLinearCurve(48000, 24000, 0);

            CollectionAssert.AreEqual(originalCurve, restoredCurve);
        }

        /// <summary>
        /// Tests if Base64 round-trip preserves <see cref="RoomCurveLikeCurve.GenerateLogCurve(double, double, int, float)"/>.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void Base64RoundTripPreservesLogCurve() {
            RoomCurveLikeCurve original = new RoomCurveLikeCurve(150, 5, 25);
            string base64 = original.ToBase64();
            RoomCurveLikeCurve restored = new RoomCurveLikeCurve(200, 3);
            restored.FromBase64(base64);

            float[] originalCurve = original.GenerateLogCurve(10, 20000, 100, 0);
            float[] restoredCurve = restored.GenerateLogCurve(10, 20000, 100, 0);

            CollectionAssert.AreEqual(originalCurve, restoredCurve);
        }
    }
}
