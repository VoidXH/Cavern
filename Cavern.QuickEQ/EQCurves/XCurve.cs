using Cavern.Filters;
using System;
using System.Runtime.CompilerServices;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Cinema standard EQ curve.
    /// </summary>
    public class XCurve : EQCurve {
        /// <summary>
        /// Hardcoded log10(2000) (mid knee position), as C# compilers don't optimize this.
        /// </summary>
        const double log10_2000 = 3.30102999566;

        /// <summary>
        /// Hardcoded log10(10000) (high knee position), as C# compilers don't optimize this.
        /// </summary>
        const double log10_10000 = 4;

        /// <summary>
        /// Hardcoded log10(20000) (high extension position helper), as C# compilers don't optimize this.
        /// </summary>
        const double log10_20000 = 4.30102999566f;

        /// <summary>
        /// Hardcoded 1 / (log10(10000) - log10(2000)) for mid slope positioning.
        /// </summary>
        const double midMul = 1.43067655807;

        /// <summary>
        /// Hardcoded 1 / (log10(20000) - log10(10000)) for high slope division.
        /// </summary>
        const double highMul = 3.32192809489;

        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] {
            get {
                if (frequency < 2000) {
                    return 0;
                }
                if (frequency < 10000) {
                    return -6 * (Math.Log10(frequency) - log10_2000) * midMul;
                }
                return -6 * ((Math.Log10(frequency) - log10_10000) * highMul) - 6;
            }
        }

        /// <summary>
        /// Generate a linear curve for correction generators with an additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            int midKnee = length * 2000 / sampleRate,
                highKnee = length * 10000 / sampleRate;
            FinalizeKnees(ref midKnee, ref highKnee, length);
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            Array.Fill(curve, gain, 0, midKnee);
            for (int pos = midKnee; pos < highKnee; pos++) {
                curve[pos] = -6 / 8000f * (pos * positioner - 2000) + gain;
            }
            gain -= 6;
            for (int pos = highKnee; pos < length; pos++) {
                curve[pos] = -6 / 10000f * (pos * positioner - 10000) + gain;
            }
            return curve;
        }

        /// <summary>
        /// Generate a logarithmic curve for correction generators with an additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = new float[length];
            double powerMin = Math.Log10(startFreq), powerRange = length / (Math.Log10(endFreq) - powerMin);
            int midKnee = (int)((log10_2000 - powerMin) * powerRange), highKnee = (int)((log10_10000 - powerMin) * powerRange);
            FinalizeKnees(ref midKnee, ref highKnee, length);
            Array.Fill(curve, gain, 0, midKnee);
            float positioner = 1f / (highKnee - midKnee);
            for (int pos = midKnee; pos < highKnee; pos++) {
                curve[pos] = -6 * (pos - midKnee) * positioner + gain;
            }
            positioner = (float)(1.0 / ((log10_20000 - powerMin) * powerRange - highKnee));
            gain -= 6;
            for (int pos = highKnee; pos < length; pos++) {
                curve[pos] = -6 * (pos - highKnee) * positioner + gain;
            }
            return curve;
        }

        /// <summary>
        /// Set the knees' positions within limits in case the curve is too short.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FinalizeKnees(ref int midKnee, ref int highKnee, int length) {
            if (midKnee < 0) {
                midKnee = 0;
                if (highKnee < 0) {
                    highKnee = 0;
                }
            }
            if (highKnee > length) {
                highKnee = length;
                if (midKnee > length) {
                    midKnee = length;
                }
            }
        }
    }
}