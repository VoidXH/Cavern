using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>EQ curve with a sub-bass slope for depth emphasis.</summary>
    public class Depth : EQCurve {
        /// <summary>Hardcoded log10(60), as C# compilers don't optimize this.</summary>
        const float log10_60 = 1.77815125038f;

        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public override float At(float frequency) {
            if (frequency > 60)
                return 0;
            return 12f / 60 * (60 - frequency);
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public override float[] GenerateLinearCurve(int length, int sampleRate) {
            float[] curve = new float[length];
            float positioner = 12f / 60 * sampleRate * .5f / length;
            int at60 = (int)(length * 120f / sampleRate);
            if (at60 > length)
                at60 = length;
            for (int pos = 0; pos < at60; ++pos)
                curve[pos] = 12 - pos * positioner;
            return curve;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLinearCurve(int, int)"/>, it's faster.</remarks>
        public override float[] GenerateLinearCurve(int length, int sampleRate, float gain) {
            float[] curve = new float[length];
            float positioner = 12f / 60 * sampleRate * .5f / length;
            int at60 = (int)(length * 120f / sampleRate);
            if (at60 > length)
                at60 = length;
            gain += 12;
            for (int pos = 0; pos < at60; ++pos)
                curve[pos] = gain - pos * positioner;
            for (int pos = at60; pos < length; ++pos)
                curve[pos] = gain;
            return curve;
        }

        /// <summary>Generate a logarithmic curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public override float[] GenerateLogCurve(int length, float startFreq, float endFreq) {
            float[] curve = new float[length];
            double powerMin = Math.Log10(startFreq), powerRange = (Math.Log10(endFreq) - powerMin) / length;
            int at60 = (int)((log10_60 - powerMin) / powerRange);
            if (at60 > length)
                at60 = length;
            float startGain = At(startFreq), positioner = startGain / at60;
            for (int pos = 0; pos < at60; ++pos)
                curve[pos] = startGain - pos * positioner;
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
            int at60 = (int)((log10_60 - powerMin) / powerRange);
            if (at60 > length)
                at60 = length;
            float startGain = At(startFreq), positioner = startGain / at60;
            for (int pos = 0; pos < at60; ++pos)
                curve[pos] = startGain - pos * positioner + gain;
            for (int pos = at60; pos < length; ++pos)
                curve[pos] = gain;
            return curve;
        }
    }
}