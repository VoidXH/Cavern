using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>EQ curve generation functions.</summary>
    public static class Curves {
        /// <summary>Generate a linear curve for the correction generators.</summary>
        /// <param name="Function">Curve function used for generation, built-in curves are available with <see cref="GetCurve(CurveFunctions)"/></param>
        /// <param name="Length">Curve length</param>
        /// <param name="SampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="Gain">Curve reference level</param>
        public static float[] GenerateLinearCurve(CurveFunction Function, int Length, int SampleRate, float Gain) {
            float[] Curve = new float[Length];
            float Positioner = SampleRate * .5f / Length;
            for (int Pos = 0; Pos < Length; ++Pos)
                Curve[Pos] = Gain + Function(Pos * Positioner);
            return Curve;
        }

        /// <summary>Generate a logarithmic curve for the correction generators.</summary>
        /// <param name="Function">Curve function used for generation, built-in curves are available with <see cref="GetCurve(CurveFunctions)"/></param>
        /// <param name="Length">Curve length</param>
        /// <param name="StartFreq">Frequency at the beginning of the curve</param>
        /// <param name="EndFreq">Frequency at the end of the curve</param>
        /// <param name="Gain">Curve reference level</param>
        public static float[] GenerateLogCurve(CurveFunction Function, int Length, float StartFreq, float EndFreq, float Gain) {
            float[] Curve = new float[Length];
            float PowerMin = Mathf.Log10(StartFreq), PowerRange = (Mathf.Log10(EndFreq) - PowerMin) / Length;
            for (int Pos = 0; Pos < Length; ++Pos)
                Curve[Pos] = Gain + Function(Mathf.Pow(10, PowerMin + PowerRange * Pos));
            return Curve;
        }

        /// <summary>Function describing a target EQ curve, returning gain for each frequency.</summary>
        public delegate float CurveFunction(float Frequency);

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
        static float Flat(float Frequency) => 0;

        /// <summary>Cinema standard curve.</summary>
        static float XCurve(float Frequency) {
            if (Frequency < 2000)
                return 0;
            else if (Frequency < 10000)
                return -6 / 8000f * (Frequency - 2000);
            else
                return -6 / 10000f * (Frequency - 10000) - 6;
        }

        /// <summary>Adds a bass bump for punch emphasis.</summary>
        static float Punch(float Frequency) {
            if (Frequency > 120)
                return 0;
            return (1 - Mathf.Cos(Mathf.PI * Mathf.Log(.9f / 120 * Frequency + .1f))) * 5;
        }

        /// <summary>Adds a sub-bass slope for depth emphasis.</summary>
        static float Depth(float Frequency) {
            if (Frequency > 60)
                return 0;
            return 12f / 60 * (60 - Frequency);
        }
    }
}