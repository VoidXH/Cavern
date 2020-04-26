using System;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>Equalizer curve processing.</summary>
    public abstract class EQCurve {
        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public abstract float At(float frequency);

        /// <summary>Create a curve from <see cref="CurveFunction"/> definitions.</summary>
        public static EQCurve CreateCurve(CurveFunction function) {
            switch (function) {
                case CurveFunction.XCurve:
                    return new XCurve();
                case CurveFunction.Punch:
                    return new Punch();
                case CurveFunction.Depth:
                    return new Depth();
                case CurveFunction.Bandpass:
                    throw new Exception("Bandpass EQ should be created with its constructor.");
                case CurveFunction.RoomCurve:
                    return new RoomCurve();
                default:
                    return new Flat();
            }
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        public virtual float[] GenerateLinearCurve(int length, int sampleRate) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = 0; pos < length; ++pos)
                curve[pos] = At(pos * positioner);
            return curve;
        }

        /// <summary>Generate a linear curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        /// <remarks>For uses where gain is not needed, use <see cref="GenerateLinearCurve(int, int)"/>, it's faster.</remarks>
        public virtual float[] GenerateLinearCurve(int length, int sampleRate, float gain) {
            float[] curve = new float[length];
            float positioner = sampleRate * .5f / length;
            for (int pos = 0; pos < length; ++pos)
                curve[pos] = gain + At(pos * positioner);
            return curve;
        }

        /// <summary>
        /// If you have overridden <see cref="GenerateLinearCurve(int, int)"/>, but not <see cref="GenerateLinearCurve(int, int, float)"/>,
        /// the latter should return this for increased performance.
        /// </summary>
        protected float[] GenerateLinearCurveOptimized(int length, int sampleRate, float gain) {
            float[] curve = GenerateLinearCurve(length, sampleRate, gain);
            for (int pos = 0; pos < length; ++pos)
                curve[pos] += gain;
            return curve;
        }

        /// <summary>Generate a logarithmic curve for correction generators.</summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public virtual float[] GenerateLogCurve(int length, float startFreq, float endFreq) {
            float[] curve = new float[length];
            float freqHere = startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length);
            for (int pos = 0; pos < length; ++pos) {
                curve[pos] = At(freqHere);
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
        public virtual float[] GenerateLogCurve(int length, float startFreq, float endFreq, float gain) {
            float[] curve = new float[length];
            float freqHere = startFreq, multiplier = (float)Math.Pow(endFreq / startFreq, 1f / length);
            for (int pos = 0; pos < length; ++pos) {
                curve[pos] = At(freqHere) + gain;
                freqHere *= multiplier;
            }
            return curve;
        }

        /// <summary>
        /// If you have overridden <see cref="GenerateLogCurve(int, float, float)"/>, but not <see cref="GenerateLogCurve(int, float, float, float)"/>,
        /// the latter should return this for increased performance.
        /// </summary>
        protected float[] GenerateLogCurveOptimized(int length, int sampleRate, float gain) {
            float[] curve = GenerateLogCurve(length, sampleRate, gain);
            for (int pos = 0; pos < length; ++pos)
                curve[pos] += gain;
            return curve;
        }
    }
}