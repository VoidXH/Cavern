using Cavern.QuickEQ.EQCurves;

namespace Test.Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Helpers for testing <see cref="EQCurve"/>s.
    /// </summary>
    static class EQCurveTestUtils {
        /// <summary>
        /// Tests if a <see cref="RoomCurveLikeCurve"/>'s this operator works as intended.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public static void RoomCurveLikeThis(RoomCurveLikeCurve curve, int riseFreq, int riseGain, float trebleSuppression) {
            Assert.AreEqual(riseGain, curve[20], 2 * Consts.delta); // 20 Hz: 3 dB
            Assert.AreEqual(0, curve[riseFreq], Consts.delta); // 200 Hz: 0 dB
            Assert.AreEqual(0, curve[1000], Consts.delta); // 1 kHz: 0 dB
            Assert.AreEqual(-trebleSuppression, curve[20000], Consts.delta); // 20 kHz: treble suppressed
        }

        /// <summary>
        /// Tests if <see cref="RoomCurveLikeCurve.GenerateLinearCurve(int, int, float)"/> works as intended.
        /// </summary>
        public static void RoomCurveLikeGenerateLinearCurve(RoomCurveLikeCurve curve, int riseFreq, int riseGain, float trebleSuppression) {
            const int divider = 20; // Resolution will be this many times lower than 1 Hz
            float[] linear = curve.GenerateLinearCurve(48000, 24000 / divider);
            Assert.AreEqual(riseGain, linear[20 / divider], Consts.delta); // 20 Hz: 3 dB
            Assert.AreNotEqual(0, linear[riseFreq / divider - 1], Consts.delta); // Before 200 Hz: not 0 dB
            Assert.AreEqual(0, linear[200 / divider], Consts.delta); // 200 Hz: 0 dB
            Assert.AreEqual(0, linear[1000 / divider], Consts.delta); // 1 kHz: 0 dB
            Assert.AreNotEqual(0, linear[1000 / divider + 1], Consts.delta); // After 1 kHz: not 0 dB
            Assert.AreEqual(-trebleSuppression, linear[20000 / divider], Consts.delta); // 20 kHz: treble suppressed
        }
    }
}