using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>EQ curve with a bass bump for punch emphasis.</summary>
    public class Punch : EQCurve {
        /// <summary>Hardcoded log10(120), as C# compilers don't optimize this.</summary>
        const float log10_120 = 2.07918124605f;

        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public override float At(float frequency) {
            if (frequency > 120)
                return 0;
            return (float)(1 - Math.Cos(2 * Math.PI / 120 * frequency)) * 6;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public override float[] GenerateLinearCurve(int length, int sampleRate) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            int at120 = (int)(length * 240f / sampleRate);
            if (at120 > length)
                at120 = length;
            for (int pos = 0; pos < at120; ++pos)
                curve[pos] = (float)(1 - Math.Cos(2 * Math.PI / 120 * pos * positioner)) * 6;
            return curve;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLinearCurve(int, int)"/>, it's faster.</remarks>
        public override float[] GenerateLinearCurve(int length, int sampleRate, float gain) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            int at120 = (int)(length * 240f / sampleRate);
            if (at120 > length)
                at120 = length;
            for (int pos = 0; pos < at120; ++pos)
                curve[pos] = gain + (float)(1 - Math.Cos(2 * Math.PI / 120 * pos * positioner)) * 6;
            for (int pos = at120; pos < length; ++pos)
                curve[pos] = gain;
            return curve;
        }

        /// <summary>Generate a logarithmic curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public override float[] GenerateLogCurve(int length, float startFreq, float endFreq) {
            float[] curve = new float[length];
            float freqHere = startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length), powerMin = (float)Math.Log10(startFreq);
            int at120 = (int)((log10_120 - powerMin) / (Math.Log10(endFreq) - powerMin) * length);
            for (int pos = 0; pos < at120; ++pos) {
                curve[pos] = (float)(1 - Math.Cos(2 * Math.PI / 120 * freqHere)) * 6;
                freqHere *= multiplier;
            }
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
            float freqHere = startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length), powerMin = (float)Math.Log10(startFreq);
            int at120 = (int)((log10_120 - powerMin) / (Math.Log10(endFreq) - powerMin) * length);
            for (int pos = 0; pos < at120; ++pos) {
                curve[pos] = (float)(1 - Math.Cos(2 * Math.PI / 120 * freqHere)) * 6 + gain;
                freqHere *= multiplier;
            }
            for (int pos = at120; pos < length; ++pos)
                curve[pos] = gain;
            return curve;
        }
    }
}