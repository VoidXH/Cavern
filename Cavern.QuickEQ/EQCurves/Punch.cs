using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// EQ curve with a bass bump for punch emphasis.
    /// </summary>
    public class Punch : EQCurve {
        /// <summary>
        /// Hardcoded log10(120), as C# compilers don't optimize this.
        /// </summary>
        const float log10_120 = 2.07918124605f;

        /// <summary>
        /// Filter gain in decibels.
        /// </summary>
        public double Gain { get; private set; }

        /// <summary>
        /// EQ curve with a 6 dB bass bump for punch emphasis.
        /// </summary>
        public Punch() => Gain = 3;

        /// <summary>
        /// EQ curve with a bass bump at custom gain for punch emphasis.
        /// </summary>
        public Punch(double gain) => Gain = gain / 2;

        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] {
            get {
                if (frequency > 120) {
                    return 0;
                }
                return (1 - Math.Cos(2 * Math.PI / 120 * frequency)) * Gain;
            }
        }

        /// <summary>
        /// Generate a linear curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            int at120 = (int)(length * 240f / sampleRate);
            if (at120 > length) {
                at120 = length;
            }
            for (int pos = 0; pos < at120; pos++) {
                curve[pos] = gain + (float)((1 - Math.Cos(2 * Math.PI / 120 * pos * positioner)) * Gain);
            }
            Array.Fill(curve, gain, at120, length - at120);
            return curve;
        }

        /// <summary>
        /// Generate a logarithmic curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = new float[length];
            float freqHere = (float)startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length),
                powerMin = (float)Math.Log10(startFreq);
            int at120 = (int)((log10_120 - powerMin) / (Math.Log10(endFreq) - powerMin) * length);
            if (at120 > length) {
                at120 = length;
            }
            for (int pos = 0; pos < at120; pos++) {
                curve[pos] = (float)((1 - Math.Cos(2 * Math.PI / 120 * freqHere)) * Gain) + gain;
                freqHere *= multiplier;
            }
            Array.Fill(curve, gain, at120, length - at120);
            return curve;
        }
    }
}