using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// An EQ curve with any amount of custom bands.
    /// </summary>
    public class Custom : EQCurve {
        /// <summary>
        /// Equalization source.
        /// </summary>
        readonly Equalizer eq;

        /// <summary>
        /// Create a custom EQ curve from a source stored as an Equalier.
        /// </summary>
        public Custom(Equalizer eq) => this.eq = eq;

        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] => eq[frequency];

        /// <summary>
        /// Generate a linear curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public override float[] GenerateLinearCurve(int sampleRate, int length) => eq.VisualizeLinear(0, sampleRate * .5, length);

        /// <summary>
        /// Generate a linear curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLinearCurve(int, int)"/>, it's faster.</remarks>
        public override float[] GenerateLinearCurve(int sampleRate, int length, float gain) =>
            GenerateLinearCurveOptimized(sampleRate, length, gain);

        /// <summary>
        /// Generate a logarithmic curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length) => eq.Visualize(startFreq, endFreq, length);

        /// <summary>
        /// Generate a logarithmic curve for correction generators.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLogCurve(double, double, int)"/>, it's faster.</remarks>
        public override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) =>
            GenerateLogCurveOptimized(startFreq, endFreq, length, gain);
    }
}