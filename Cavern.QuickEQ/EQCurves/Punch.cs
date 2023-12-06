using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// EQ curve with a bass bump for punch emphasis.
    /// </summary>
    public class Punch : TrebleSuppressingCurve {
        /// <summary>
        /// Filter gain in decibels.
        /// </summary>
        public double Gain { get; private set; } = 3;

        /// <summary>
        /// EQ curve with a 6 dB bass bump for punch emphasis.
        /// </summary>
        public Punch() => Gain = 3;

        /// <summary>
        /// EQ curve with a bass bump at custom gain for punch emphasis.
        /// </summary>
        public Punch(double gain) => Gain = gain / 2;

        /// <inheritdoc/>
        public override double this[double frequency] {
            get {
                if (frequency > 120) {
                    return base[frequency];
                }
                return (1 - Math.Cos(2 * Math.PI / 120 * frequency)) * Gain;
            }
        }

        /// <inheritdoc/>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = base.GenerateLinearCurve(sampleRate, length, gain);
            float positioner = sampleRate * .5f / length;
            int at120 = (int)(length * 240f / sampleRate);
            if (at120 > length) {
                at120 = length;
            }
            for (int pos = 0; pos < at120; pos++) {
                curve[pos] = gain + (float)((1 - Math.Cos(2 * Math.PI / 120 * pos * positioner)) * Gain);
            }
            return curve;
        }

        /// <inheritdoc/>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = base.GenerateLogCurve(startFreq, endFreq, length, gain);
            float freqHere = (float)startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length),
                powerMin = (float)Math.Log10(startFreq);
            const float log10_120 = 2.07918124605f;
            int at120 = (int)((log10_120 - powerMin) / (Math.Log10(endFreq) - powerMin) * length);
            if (at120 > length) {
                at120 = length;
            }
            for (int pos = 0; pos < at120; pos++) {
                curve[pos] = (float)((1 - Math.Cos(2 * Math.PI / 120 * freqHere)) * Gain) + gain;
                freqHere *= multiplier;
            }
            return curve;
        }
    }
}