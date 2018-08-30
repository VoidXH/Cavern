using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>EQ curve generation functions.</summary>
    public static class Curves {
        /// <summary>Generate a linear cinema standard reference curve (X-curve) for the correction generators.</summary>
        /// <param name="Length">Curve length</param>
        /// <param name="SampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="Gain">Level of the flat part of the curve</param>
        public static float[] GenerateLinearXCurve(int Length, int SampleRate, float Gain) {
            float[] Curve = new float[Length];
            float Positioner = SampleRate * .5f / Length;
            for (int Pos = 0; Pos < Length; ++Pos)
                Curve[Pos] = Gain + XGain(Pos * Positioner);
            return Curve;
        }

        /// <summary>Generate a logarithmic cinema standard reference curve (X-curve) for the correction generators.</summary>
        /// <param name="Length">Curve length</param>
        /// <param name="StartFreq">Frequency at the beginning of the curve</param>
        /// <param name="EndFreq">Frequency at the end of the curve</param>
        /// <param name="Gain">Level of the flat part of the curve</param>
        public static float[] GenerateLogXCurve(int Length, float StartFreq, float EndFreq, float Gain) {
            float[] Curve = new float[Length];
            float PowerMin = Mathf.Log10(StartFreq), PowerRange = (Mathf.Log10(EndFreq) - PowerMin) / Length;
            for (int Pos = 0; Pos < Length; ++Pos)
                Curve[Pos] = Gain + XGain(Mathf.Pow(10, PowerMin + PowerRange * Pos));
            return Curve;
        }

        /// <summary>Get the gain of the X-curve at a given frequency.</summary>
        static float XGain(float FreqHere) {
            if (FreqHere < 2000)
                return 0;
            else if (FreqHere < 10000)
                return MidMul * (FreqHere - 2000);
            else
                return EndMul * (FreqHere - 10000) - 6;
        }
        const float MidMul = -6 / 8000f, EndMul = -6 / 10000f; // Optimizer variables for the previous function

        /// <summary>Generate a linear IMAX reference curve for the correction generators.</summary>
        /// <param name="Length">Curve length</param>
        /// <param name="SampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="Gain">Level of the flat part of the curve</param>
        public static float[] GenerateLinearIMAXCurve(int Length, int SampleRate, float Gain) {
            float[] Curve = new float[Length];
            int BellEnd = 120 * Length / (SampleRate / 2);
            float EndDiv = Measurements.Pix2 / BellEnd;
            for (int Pos = 0; Pos < BellEnd; ++Pos)
                Curve[Pos] = (1 - Mathf.Cos(Pos * EndDiv)) * 5 + Gain;
            for (int Pos = BellEnd; Pos < Length; ++Pos)
                Curve[Pos] = Gain;
            return Curve;
        }

        /// <summary>Generate a logarithmic IMAX reference curve for the correction generators.</summary>
        /// <param name="Length">Curve length</param>
        /// <param name="StartFreq">Frequency at the beginning of the curve</param>
        /// <param name="EndFreq">Frequency at the end of the curve</param>
        /// <param name="Gain">Level of the flat part of the curve</param>
        public static float[] GenerateLogIMAXCurve(int Length, float StartFreq, float EndFreq, float Gain) {
            float[] Curve = new float[Length];
            float PowerMin = Mathf.Log10(StartFreq), PowerRange = (Mathf.Log10(EndFreq) - PowerMin) / Length;
            int BellStart = Mathf.RoundToInt((1 - PowerMin) / PowerRange), BellEnd = Mathf.RoundToInt((Log120 - PowerMin) / PowerRange);
            float EndDiv = Measurements.Pix2 / (BellEnd - BellStart);
            for (int Pos = 0; Pos < BellEnd; ++Pos)
                Curve[Pos] = (1 - Mathf.Cos((Pos - BellStart) * EndDiv)) * 5 + Gain;
            for (int Pos = BellEnd; Pos < Length; ++Pos)
                Curve[Pos] = Gain;
            return Curve;
        }
        const float Log120 = 2.0791812460476248277225056927041f; // Optimizer variable for the previous function
    }
}