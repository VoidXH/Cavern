using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>Cinema standard EQ curve.</summary>
    public class XCurve : EQCurve {
        /// <summary>Hardcoded log10(2000) (mid knee position), as C# compilers don't optimize this.</summary>
        const float log10_2000 = 3.30102999566f;
        /// <summary>Hardcoded log10(10000) (high knee position), as C# compilers don't optimize this.</summary>
        const float log10_10000 = 4;
        /// <summary>Hardcoded log10(10000) - log10(2000) for mid slope division.</summary>
        const float midDiv = .69897000433f;
        /// <summary>Hardcoded log10(20000) - log10(10000) for high slope division.</summary>
        const float highDiv = .30102999566f;

        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public override float At(float frequency) {
            if (frequency < 2000)
                return 0;
            if (frequency < 10000)
                return -6 * (float)((Math.Log(frequency, 10) - log10_2000) / midDiv);
            else
                return -6 * (float)((Math.Log(frequency, 10) - log10_10000) / highDiv) - 6;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public override float[] GenerateLinearCurve(int length, int sampleRate) {
            int midKnee = length * 2000 / sampleRate, highKnee = length * 10000 / sampleRate;
            if (highKnee > length) {
                highKnee = length;
                if (midKnee > length)
                    midKnee = length;
            }
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = midKnee; pos < highKnee; ++pos)
                curve[pos] = -6 / 8000f * (pos * positioner - 2000);
            for (int pos = highKnee; pos < length; ++pos)
                curve[pos] = -6 / 10000f * (pos * positioner - 10000) - 6;
            return curve;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLinearCurve(int, int)"/>, it's faster.</remarks>
        public override float[] GenerateLinearCurve(int length, int sampleRate, float gain) {
            int midKnee = length * 2000 / sampleRate, highKnee = length * 10000 / sampleRate;
            if (highKnee > length) {
                highKnee = length;
                if (midKnee > length)
                    midKnee = length;
            }
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = 0; pos < midKnee; ++pos)
                curve[pos] = gain;
            for (int pos = midKnee; pos < highKnee; ++pos)
                curve[pos] = -6 / 8000f * (pos * positioner - 2000) + gain;
            gain -= 6;
            for (int pos = highKnee; pos < length; ++pos)
                curve[pos] = -6 / 10000f * (pos * positioner - 10000) + gain;
            return curve;
        }

        /// <summary>Generate a logarithmic curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public override float[] GenerateLogCurve(int length, float startFreq, float endFreq) {
            float[] curve = new float[length];
            double powerMin = Math.Log10(startFreq), powerRange = (Math.Log10(endFreq) - powerMin) / length;
            int midKnee = (int)((log10_2000 - powerMin) / powerRange), highKnee = (int)((log10_10000 - powerMin) / powerRange);
            if (highKnee > length) {
                highKnee = length;
                if (midKnee > length)
                    midKnee = length;
            }
            float positioner = 1f / (highKnee - midKnee);
            for (int pos = midKnee; pos < highKnee; ++pos)
                curve[pos] = -6 * (pos - midKnee) * positioner;
            positioner = 1f / (length - highKnee);
            for (int pos = highKnee; pos < length; ++pos)
                curve[pos] = -6 * (pos - highKnee) * positioner - 6;
            return curve;
        }

        /// <summary>Generate a logarithmic curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLogCurve(int, float, float)"/>, it's faster.</remarks>
        public override float[] GenerateLogCurve(int length, float startFreq, float endFreq, float gain) {
            float[] curve = new float[length];
            double powerMin = Math.Log10(startFreq), powerRange = (Math.Log10(endFreq) - powerMin) / length;
            int midKnee = (int)((log10_2000 - powerMin) / powerRange), highKnee = (int)((log10_10000 - powerMin) / powerRange);
            if (highKnee > length) {
                highKnee = length;
                if (midKnee > length)
                    midKnee = length;
            }
            for (int pos = 0; pos < midKnee; ++pos)
                curve[pos] = gain;
            float positioner = 1f / (highKnee - midKnee);
            for (int pos = midKnee; pos < highKnee; ++pos)
                curve[pos] = -6 * (pos - midKnee) * positioner + gain;
            positioner = 1f / (length - highKnee);
            gain -= 6;
            for (int pos = highKnee; pos < length; ++pos)
                curve[pos] = -6 * (pos - highKnee) * positioner + gain;
            return curve;
        }
    }
}