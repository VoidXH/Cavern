using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// A curve with a linear bass rise and linear treble suppression.
    /// </summary>
    public class RoomCurveLikeCurve : TrebleSuppressingCurve {
        /// <summary>
        /// The frequency from where the bass rise starts.
        /// </summary>
        readonly double kneeFrequency;

        /// <summary>
        /// Cached low knee position on logarithmic scale.
        /// </summary>
        readonly double kneePosition;

        /// <summary>
        /// Bass rise at 20 Hz in decibels.
        /// </summary>
        readonly float rise;

        /// <summary>
        /// The multiplier for getting the gain for a given frequency in the index operator.
        /// </summary>
        readonly double singleFreqHelper;

        /// <summary>
        /// A curve with a linear bass rise and linear treble suppression.
        /// </summary>
        /// <param name="kneeFrequency">The frequency from where the bass rise starts</param>
        /// <param name="rise">Bass rise at 20 Hz in decibels</param>
        public RoomCurveLikeCurve(double kneeFrequency, float rise) {
            this.kneeFrequency = kneeFrequency;
            kneePosition = Math.Log10(kneeFrequency);
            this.rise = rise;
            singleFreqHelper = rise / (Math.Log10(kneeFrequency) - log10_20);
        }

        /// <inheritdoc/>
        public sealed override double this[double frequency] {
            get {
                if (frequency < kneeFrequency) {
                    return singleFreqHelper * (kneePosition - Math.Log10(frequency));
                }
                return base[frequency];
            }
        }

        /// <inheritdoc/>
        public sealed override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = base.GenerateLinearCurve(sampleRate, length, gain);
            int knee = (int)(2 * kneeFrequency * curve.Length / sampleRate + .5f);
            if (knee > curve.Length) {
                knee = curve.Length;
            }

            float positioner = sampleRate * .5f / curve.Length,
                kneeFreq = (float)kneeFrequency,
                scale = -rise / (kneeFreq - 20);
            for (int i = 0; i < knee; i++) {
                curve[i] = scale * (i * positioner - kneeFreq) + gain;
            }
            return curve;
        }

        /// <inheritdoc/>
        public sealed override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = base.GenerateLogCurve(startFreq, endFreq, length, gain);
            double powerMin = Math.Log10(startFreq), powerRange = curve.Length / (Math.Log10(endFreq) - powerMin);
            int knee = (int)((Math.Log10(kneeFrequency) - powerMin) * powerRange);
            if (knee < 0) {
                return curve;
            } else if (knee > curve.Length) {
                knee = curve.Length;
            }

            float positioner = (float)(1.0 / (knee - (log10_20 - powerMin) * powerRange));
            for (int i = 0; i < knee; i++) {
                curve[i] = rise * (knee - i) * positioner + gain;
            }
            return curve;
        }

        /// <summary>
        /// Hardcoded log10(20) (low extension position on log scale).
        /// </summary>
        const double log10_20 = 1.30102999566f;
    }
}