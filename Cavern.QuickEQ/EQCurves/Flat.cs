using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// EQ curve with uniform gain on all frequencies.
    /// </summary>
    public class Flat : EQCurve {
        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] => 0;

        /// <summary>
        /// Generate a linear curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length) => new float[length];

        /// <summary>
        /// Generate a linear curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = new float[length];
            Array.Fill(curve, gain);
            return curve;
        }

        /// <summary>
        /// Generate a logarithmic curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length) => new float[length];

        /// <summary>
        /// Generate a logarithmic curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = new float[length];
            Array.Fill(curve, gain);
            return curve;
        }
    }
}