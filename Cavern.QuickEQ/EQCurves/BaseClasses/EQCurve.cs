using System;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Equalizer curve processing.
    /// </summary>
    public abstract class EQCurve : IBase64Serializable {
        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public abstract double this[double frequency] { get; }

        /// <summary>
        /// Create a curve from <see cref="CurveFunction"/> definitions.
        /// </summary>
        public static EQCurve CreateCurve(CurveFunction function) {
            switch (function) {
                case CurveFunction.XCurve:
                    return new XCurve();
                case CurveFunction.Punch:
                    return new Punch();
                case CurveFunction.Depth:
                    return new Depth();
                case CurveFunction.RoomCurve:
                    return new RoomCurve();
                case CurveFunction.Bandpass:
                case CurveFunction.Custom:
                case CurveFunction.Smoother:
                    throw new NonGeneralizedCurveException();
                default:
                    return new Flat();
            }
        }

        /// <summary>
        /// Deserialize an <see cref="EQCurve"/> from a base64 string.
        /// </summary>
        /// <param name="source">Base64 string produced by <see cref="ToBase64"/></param>
        public static EQCurve Deserialize(string source) {
            string data = FromBase64String(source);
            int separator = data.IndexOf('|');
            string typeName = separator < 0 ? data : data[..separator];
            string curveData = separator < 0 ? "" : data[(separator + 1)..];
            EQCurve curve = typeName switch {
                nameof(Flat) => new Flat(),
                nameof(AutoRoomCurve) => null,
                nameof(Bandpass) => new Bandpass(20, 20000, 48000, 1024),
                nameof(Custom) => new Custom(new Equalizer()),
                nameof(Depth) => new Depth(),
                nameof(Punch) => new Punch(),
                nameof(RoomCurveLikeCurve) => null,
                nameof(Smoother) => throw new NonGeneralizedCurveException(),
                nameof(XCurve) => new XCurve(),
                _ => throw new NotImplementedException()
            };
            if (curve != null) {
                curve.FromBase64(source);
                return curve;
            }

            if (typeName == nameof(AutoRoomCurve) || typeName == nameof(RoomCurve) || typeName == nameof(RoomCurveLikeCurve)) {
                string[] roomParts = curveData.Split('|');
                if (roomParts.Length == 2 && float.TryParse(roomParts[0], out float treble) && float.TryParse(roomParts[1], out float rise)) {
                    return new RoomCurveLikeCurve(RoomCurveLikeCurve.defaultKnee, rise) { trebleSuppression = treble };
                }
            }
            throw new FormatException("Invalid curve data.");
        }

        /// <summary>
        /// Helper to convert a base64 string back to its original string.
        /// </summary>
        protected static string FromBase64String(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        /// <summary>
        /// Helper to convert a string to base64.
        /// </summary>
        protected static string ToBase64String(string data) => Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

        /// <inheritdoc/>
        public abstract void FromBase64(string source);

        /// <inheritdoc/>
        public abstract string ToBase64();

        /// <summary>
        /// Generate the filter graph as an <see cref="Equalizer"/> with a given number of logarithmically spaced <paramref name="bands"/>.
        /// </summary>
        public virtual Equalizer GenerateLogEqualizer(double startFreq, double endFreq, int bands) {
            float[] curve = GenerateLogCurve(startFreq, endFreq, bands);
            return EQGenerator.FromGraph(curve, startFreq, endFreq);
        }

        /// <summary>
        /// Generate a linear curve for correction generators with no additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual float[] GenerateLinearCurve(int sampleRate, int length) => GenerateLinearCurve(sampleRate, length, 0);

        /// <summary>
        /// Generate a linear curve for correction generators with an additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="sampleRate">Sample rate of the measurement that the generated curve will be used for</param>
        /// <param name="gain">Curve reference level</param>
        public virtual float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = new float[length];
            double positioner = sampleRate * .5 / length;
            for (int pos = 0; pos < length; pos++) {
                curve[pos] = (float)this[pos * positioner] + gain;
            }
            return curve;
        }

        /// <summary>
        /// If you have overridden <see cref="GenerateLinearCurve(int, int)"/>,
        /// but not <see cref="GenerateLinearCurve(int, int, float)"/>,
        /// the latter should return this for increased performance.
        /// </summary>
        protected float[] GenerateLinearCurveOptimized(int sampleRate, int length, float gain) {
            float[] curve = GenerateLinearCurve(sampleRate, length);
            for (int pos = 0; pos < length; pos++) {
                curve[pos] += gain;
            }
            return curve;
        }

        /// <summary>
        /// Generate a logarithmic curve for correction generators with no additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        public virtual float[] GenerateLogCurve(double startFreq, double endFreq, int length) =>
            GenerateLogCurve(startFreq, endFreq, length, 0);

        /// <summary>
        /// Generate a logarithmic curve for correction generators with an additional gain.
        /// </summary>
        /// <param name="length">Curve length</param>
        /// <param name="startFreq">Frequency at the beginning of the curve</param>
        /// <param name="endFreq">Frequency at the end of the curve</param>
        /// <param name="gain">Curve reference level</param>
        public virtual float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = new float[length];
            double freqHere = startFreq,
                multiplier = Math.Pow(endFreq / startFreq, 1.0 / (length - 1));
            for (int pos = 0; pos < length; pos++) {
                curve[pos] = (float)this[freqHere] + gain;
                freqHere *= multiplier;
            }
            return curve;
        }

        /// <summary>
        /// If you have overridden <see cref="GenerateLogCurve(double, double, int)"/>, but not
        /// <see cref="GenerateLogCurve(double, double, int, float)"/>, the latter should return this for increased performance.
        /// </summary>
        protected float[] GenerateLogCurveOptimized(double startFreq, double endFreq, int length, float gain) {
            float[] curve = GenerateLogCurve(startFreq, endFreq, length);
            for (int pos = 0; pos < length; pos++) {
                curve[pos] += gain;
            }
            return curve;
        }

        /// <summary>
        /// Get the average gain (calculated in voltage scale, returned in dB scale) of this curve
        /// with a given <paramref name="resolution"/> in Hz.
        /// </summary>
        public double GetAverageLevel(double startFreq, double endFreq, double resolution) {
            if (resolution <= 0) {
                throw new ArgumentOutOfRangeException(nameof(resolution), "Resolution can only be a positive number.");
            }

            double sum = 0;
            int n = 0;
            while (startFreq < endFreq) {
                sum += Math.Pow(10, this[startFreq] * .05);
                startFreq += resolution;
                n++;
            }
            return 20 * Math.Log10(sum / n);
        }
    }
}