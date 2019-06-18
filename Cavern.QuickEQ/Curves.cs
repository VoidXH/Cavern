using System;

namespace Cavern.QuickEQ {
    /// <summary>EQ curve generation functions.</summary>
    public static class Curves {
        /// <summary>Generate a linear curve for the correction generators.</summary>
        /// <param name="function">Curve function used for generation, built-in curves are available with <see cref="GetCurve(CurveFunctions)"/></param>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public static float[] GenerateLinearCurve(CurveFunction function, int length, int sampleRate, float gain) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = 0; pos < length; ++pos)
                curve[pos] = gain + function(pos * positioner);
            return curve;
        }

        /// <summary>Generate a logarithmic curve for the correction generators.</summary>
        /// <param name="function">Curve function used for generation, built-in curves are available with <see cref="GetCurve(CurveFunctions)"/></param>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        public static float[] GenerateLogCurve(CurveFunction function, int length, float startFreq, float endFreq, float gain) {
            float[] curve = new float[length];
            double powerMin = Math.Log10(startFreq), powerRange = (Math.Log10(endFreq) - powerMin) / length;
            for (int pos = 0; pos < length; ++pos)
                curve[pos] = gain + function((float)Math.Pow(10, powerMin + powerRange * pos));
            return curve;
        }

        /// <summary>Function describing a target EQ curve, returning gain for each frequency.</summary>
        public delegate float CurveFunction(float frequency);

        /// <summary>Available built-in target curves.</summary>
        public enum CurveFunctions {
            /// <summary>Uniform gain on all frequencies.</summary>
            Flat,
            /// <summary>Cinema standard curve.</summary>
            XCurve,
            /// <summary>Adds a bass bump for punch emphasis.</summary>
            Punch,
            /// <summary>Adds a sub-bass slope for depth emphasis.</summary>
            Depth
        }

        /// <summary>Get the function of built-in target curves.</summary>
        public static CurveFunction GetCurve(CurveFunctions Curve) {
            switch (Curve) {
                case CurveFunctions.XCurve:
                    return XCurve;
                case CurveFunctions.Punch:
                    return Punch;
                case CurveFunctions.Depth:
                    return Depth;
                default:
                    return Flat;
            }
        }

        /// <summary>Uniform gain on all frequencies.</summary>
        static float Flat(float frequency) => 0;

        /// <summary>Cinema standard curve.</summary>
        static float XCurve(float frequency) {
            if (frequency < 2000)
                return 0;
            else if (frequency < 10000)
                return -6 / 8000f * (frequency - 2000);
            else
                return -6 / 10000f * (frequency - 10000) - 6;
        }

        /// <summary>Adds a bass bump for punch emphasis.</summary>
        static float Punch(float frequency) {
            if (frequency > 120)
                return 0;
            return (float)(1 - Math.Cos(Math.PI * Math.Log(.9f / 120 * frequency + .1f))) * 5;
        }

        /// <summary>Adds a sub-bass slope for depth emphasis.</summary>
        static float Depth(float frequency) {
            if (frequency > 60)
                return 0;
            return 12f / 60 * (60 - frequency);
        }
    }
}