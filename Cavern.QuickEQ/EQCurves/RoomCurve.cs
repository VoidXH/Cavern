using System;
using System.Runtime.CompilerServices;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Frequently used target curve for very small rooms.
    /// </summary>
    public class RoomCurve : EQCurve {
        /// <summary>
        /// Hardcoded log10(20) (low extension position helper), as C# compilers don't optimize this.
        /// </summary>
        const double log10_20 = 1.30102999566f;

        /// <summary>
        /// Hardcoded log10(200) (low knee position), as C# compilers don't optimize this.
        /// </summary>
        const double log10_200 = 2.30102999566f;

        /// <summary>
        /// Hardcoded log10(1000) (high knee position), as C# compilers don't optimize this.
        /// </summary>
        const double log10_1000 = 3;

        /// <summary>
        /// Hardcoded log10(20000) (high extension position helper), as C# compilers don't optimize this.
        /// </summary>
        const double log10_20000 = 4.30102999566f;

        /// <summary>
        /// Hardcoded 1 / (log10(20000) - log10(1000)) for high slope division.
        /// </summary>
        const double highMul = 0.76862178684;

        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] {
            get {
                if (frequency < 200) {
                    return -3 * (Math.Log10(frequency) - log10_200);
                }
                if (frequency < 1000) {
                    return 0;
                }
                return -3 * (Math.Log10(frequency) - log10_1000) * highMul;
            }
        }

        /// <summary>
        /// Generate a linear curve for correction generators with an additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            int lowKnee = length * 200 / sampleRate, highKnee = length * 1000 / sampleRate;
            FinalizeKnees(ref lowKnee, ref highKnee, length);
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = 0; pos < lowKnee; pos++) {
                curve[pos] = -3 / 180f * (pos * positioner - 200) + gain;
            }
            Array.Fill(curve, gain, lowKnee, highKnee - lowKnee);
            for (int pos = highKnee; pos < length; pos++) {
                curve[pos] = -3 / 19000f * (pos * positioner - 1000) + gain;
            }
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
            double powerMin = Math.Log10(startFreq), powerRange = length / (Math.Log10(endFreq) - powerMin);
            int lowKnee = (int)((log10_200 - powerMin) * powerRange), highKnee = (int)((log10_1000 - powerMin) * powerRange);
            FinalizeKnees(ref lowKnee, ref highKnee, length);
            float positioner = (float)(1.0 / (lowKnee - (log10_20 - powerMin) * powerRange));
            for (int pos = 0; pos < lowKnee; pos++) {
                curve[pos] = -3 * (pos - lowKnee) * positioner + gain;
            }
            Array.Fill(curve, gain, lowKnee, highKnee - lowKnee);
            positioner = (float)(1.0 / ((log10_20000 - powerMin) * powerRange - highKnee));
            for (int pos = highKnee; pos < length; pos++) {
                curve[pos] = -3 * (pos - highKnee) * positioner + gain;
            }
            return curve;
        }

        /// <summary>
        /// Set the knees' positions within limits in case the curve is too short.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FinalizeKnees(ref int lowKnee, ref int highKnee, int length) {
            if (lowKnee < 0) {
                lowKnee = 0;
                if (highKnee < 0) {
                    highKnee = 0;
                }
            }
            if (highKnee > length) {
                highKnee = length;
            }
            if (lowKnee > length) {
                lowKnee = length;
            }
        }
    }
}