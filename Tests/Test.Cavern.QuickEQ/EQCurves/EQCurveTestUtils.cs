using Cavern.QuickEQ.EQCurves;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.EQCurves;

/// <summary>
/// Helpers for testing <see cref="EQCurve"/>s.
/// </summary>
static class EQCurveTestUtils {
    /// <summary>
    /// Tests if a <see cref="RoomCurveLikeCurve"/>'s this operator works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public static void RoomCurveLikeIndexer(RoomCurveLikeCurve curve, int riseFreq, int riseGain, float trebleSuppression) {
        Assert.AreEqual(riseGain, curve[10], 2 * Constants.delta); // 10 Hz: clamped to 3 dB
        Assert.AreEqual(riseGain, curve[20], 2 * Constants.delta); // 20 Hz: 3 dB
        double midFreq = Math.Sqrt(2 * riseFreq); // Midpoint on log scale: log10(sqrt(20*200)) ≈ log10(63.2) ≈ 1.8, should be ~half rise
        Assert.AreEqual(riseGain / 2.0, curve[midFreq], 2 * Constants.delta); // Midpoint: half rise
        Assert.AreEqual(0, curve[riseFreq], Constants.delta); // 200 Hz: 0 dB
        Assert.AreEqual(0, curve[1000], Constants.delta); // 1 kHz: 0 dB
        Assert.AreEqual(-trebleSuppression, curve[20000], Constants.delta); // 20 kHz: treble suppressed
    }

    /// <summary>
    /// Tests if <see cref="RoomCurveLikeCurve.GenerateLinearCurve(int, int, float)"/> works as intended.
    /// </summary>
    public static void RoomCurveLikeLinearCurve(RoomCurveLikeCurve curve, int riseFreq, int riseGain, float trebleSuppression) {
        const int divisor = 20; // Resolution will be this many times lower than 1 Hz
        float[] linear = curve.GenerateLinearCurve(48000, 24000 / divisor);
        Assert.AreEqual(riseGain, linear[10 / divisor], Constants.delta); // 10 Hz: clamped to 3 dB
        Assert.AreEqual(riseGain, linear[20 / divisor], Constants.delta); // 20 Hz: 3 dB
        int midIdx = (20 + riseFreq) / 2 / divisor; // Midpoint on linear scale for the test
        Assert.AreEqual(riseGain / 2.0, linear[midIdx], Constants.delta); // Midpoint: half rise
        Assert.AreNotEqual(0, linear[riseFreq / divisor - 1], Constants.delta); // Before 200 Hz: not 0 dB
        Assert.AreEqual(0, linear[200 / divisor], Constants.delta); // 200 Hz: 0 dB
        Assert.AreEqual(0, linear[1000 / divisor], Constants.delta); // 1 kHz: 0 dB
        Assert.AreNotEqual(0, linear[1000 / divisor + 1], Constants.delta); // After 1 kHz: not 0 dB
        Assert.AreEqual(-trebleSuppression, linear[20000 / divisor], Constants.delta); // 20 kHz: treble suppressed
    }

    /// <summary>
    /// Tests if <see cref="RoomCurveLikeCurve.GenerateLogCurve(double, double, int, float)"/> works as intended.
    /// </summary>
    public static void RoomCurveLikeLogCurve(RoomCurveLikeCurve curve, int riseFreq, int riseGain, float trebleSuppression) {
        const int length = 100;
        const double startFreq = 10,
            endFreq = 20000;
        float[] logCurve = curve.GenerateLogCurve(startFreq, endFreq, length);

        double powerMin = Math.Log10(startFreq),
            powerRange = length / (Math.Log10(endFreq) - powerMin);
        int idx10 = (int)((Math.Log10(10) - powerMin) * powerRange);
        int idx20 = (int)((Math.Log10(20) - powerMin) * powerRange);
        int idxMid = (int)((Math.Log10(Math.Sqrt(20.0 * riseFreq)) - powerMin) * powerRange);
        int idxRise = (int)((Math.Log10(riseFreq) - powerMin) * powerRange);
        int idx1000 = (int)((Math.Log10(1000) - powerMin) * powerRange);
        int idx20000 = (int)((Math.Log10(20000) - powerMin) * powerRange);

        Assert.AreEqual(riseGain, logCurve[idx10], Constants.delta); // 10 Hz: clamped to 3 dB
        Assert.AreEqual(riseGain, logCurve[idx20], Constants.delta); // 20 Hz: 3 dB
        Assert.AreEqual(riseGain / 2.0, logCurve[idxMid], 2 * Constants.delta); // Midpoint: half rise
        Assert.AreEqual(0, logCurve[idxRise], Constants.delta); // 200 Hz: 0 dB
        Assert.AreEqual(0, logCurve[idx1000], Constants.delta); // 1 kHz: 0 dB
        Assert.AreEqual(-trebleSuppression, logCurve[idx20000], Constants.delta); // 20 kHz: treble suppressed
    }
}
