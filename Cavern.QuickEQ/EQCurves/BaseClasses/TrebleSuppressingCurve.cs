using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Applies the top part of the <see cref="RoomCurve"/>, to be reused in other curves not to let them be flat on the top end.
    /// The exact curve is linearly decreasing from 1 kHz to 20 kHz by a set value.
    /// </summary>
    public class TrebleSuppressingCurve : EQCurve {
        /// <summary>
        /// Linear treble decrease from 1 kHz to 20 kHz in decibels.
        /// </summary>
        public float trebleSuppression = 3;

        /// <inheritdoc/>
        public override double this[double frequency] {
            get {
                if (frequency < 1000) {
                    return 0;
                }
                return -3 * (Math.Log10(frequency) - log10_1000) * highMul;
            }
        }

        /// <inheritdoc/>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = new float[length];
            if (trebleSuppression == 0 || sampleRate < 2000) { // Knee would start from 1 kHz
                return curve;
            }
            int knee = 2 * 1000 * length / sampleRate;
            Array.Fill(curve, gain, 0, knee);

            float positioner = sampleRate * .5f / length,
                scale = -trebleSuppression / 19000;
            for (int i = knee; i < length; i++) {
                curve[i] = scale * (i * positioner - 1000) + gain;
            }
            return curve;
        }

        /// <inheritdoc/>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = new float[length];
            if (trebleSuppression == 0) {
                return curve;
            }
            double powerMin = Math.Log10(startFreq), powerRange = length / (Math.Log10(endFreq) - powerMin);
            int knee = (int)((log10_1000 - powerMin) * powerRange);
            if (knee > length) {
                knee = length;
            }
            Array.Fill(curve, gain, 0, knee);

            float positioner = (float)(1.0 / ((log10_20000 - powerMin) * powerRange - knee)),
                scale = -trebleSuppression;
            for (int pos = knee; pos < length; pos++) {
                curve[pos] = scale * (pos - knee) * positioner + gain;
            }
            return curve;
        }

        /// <summary>
        /// Hardcoded log10(1000) (high knee position on log scale).
        /// </summary>
        const double log10_1000 = 3;

        /// <summary>
        /// Hardcoded log10(20000) (high extension position on log scale).
        /// </summary>
        const double log10_20000 = 4.30102999566f;

        /// <summary>
        /// Hardcoded 1 / (log10(20000) - log10(1000)) for high slope division.
        /// </summary>
        const double highMul = 0.76862178684;
    }
}