using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// A curve with a linear bass rise and linear treble suppression.
    /// </summary>
    public class RoomCurveLikeCurve : TrebleSuppressingCurve {
        /// <summary>
        /// The frequency where the bass rise stops (default 20 Hz).
        /// </summary>
        public double BassPeakAt => Math.Pow(10, logBassPeakAt);

        /// <summary>
        /// Bass rise at <see cref="BassPeakAt"/> Hz in decibels.
        /// </summary>
        protected float rise;

        /// <summary>
        /// The frequency from where the bass rise starts.
        /// </summary>
        double kneeFrequency;

        /// <summary>
        /// Cached log10 of BassPeakAt.
        /// </summary>
        double logBassPeakAt;

        /// <summary>
        /// Cached low knee position on logarithmic scale.
        /// </summary>
        double kneePosition;

        /// <summary>
        /// The multiplier for getting the gain for a given frequency in the index operator.
        /// </summary>
        double singleFreqHelper;

        /// <inheritdoc/>
        public sealed override double this[double frequency] {
            get {
                if (frequency < kneeFrequency) {
                    double clampedFreq = Math.Max(frequency, BassPeakAt);
                    return singleFreqHelper * (kneePosition - Math.Log10(clampedFreq));
                }
                return base[frequency];
            }
        }

        /// <summary>
        /// A curve with a linear bass rise and linear treble suppression.
        /// </summary>
        /// <param name="kneeFrequency">The frequency from where the bass rise starts</param>
        /// <param name="rise">Bass rise at <see cref="BassPeakAt"/> Hz in decibels</param>
        public RoomCurveLikeCurve(double kneeFrequency, float rise) : this(kneeFrequency, rise, 20) { }

        /// <summary>
        /// A curve with a linear bass rise and linear treble suppression.
        /// </summary>
        /// <param name="kneeFrequency">The frequency from where the bass rise starts</param>
        /// <param name="rise">Bass rise at BassPeakAt Hz in decibels</param>
        /// <param name="bassPeakAt">The frequency where the bass rise stops</param>
        public RoomCurveLikeCurve(double kneeFrequency, float rise, double bassPeakAt) {
            this.rise = rise;
            this.kneeFrequency = kneeFrequency;
            logBassPeakAt = Math.Log10(bassPeakAt);
            kneePosition = Math.Log10(kneeFrequency);
            singleFreqHelper = rise / (Math.Log10(kneeFrequency) - logBassPeakAt);
        }

        /// <inheritdoc/>
        public sealed override float[] GenerateLinearCurve(int sampleRate, int length, float gain) {
            float[] curve = base.GenerateLinearCurve(sampleRate, length, gain);
            int knee = QMath.RoundToInt(2 * kneeFrequency * curve.Length / sampleRate);
            if (knee > curve.Length) {
                knee = curve.Length;
            }

            float positioner = sampleRate * .5f / curve.Length,
                scale = -rise / ((float)kneeFrequency - (float)BassPeakAt);
            for (int i = 0; i < knee; i++) {
                float freq = i * positioner;
                if (freq < BassPeakAt) {
                    freq = (float)BassPeakAt;
                }
                curve[i] = scale * (freq - (float)kneeFrequency) + gain;
            }
            return curve;
        }

        /// <inheritdoc/>
        public sealed override float[] GenerateLogCurve(double startFreq, double endFreq, int length, float gain) {
            float[] curve = base.GenerateLogCurve(startFreq, endFreq, length, gain);
            double powerMin = Math.Log10(startFreq), powerRange = curve.Length / (Math.Log10(endFreq) - powerMin);
            int knee = (int)((Math.Log10(kneeFrequency) - powerMin) * powerRange);
            if (knee < 0) {
                return curve;
            } else if (knee > curve.Length) {
                knee = length;
            }

            float positioner = (float)(1.0 / (knee - (logBassPeakAt - powerMin) * powerRange));
            for (int i = 0; i < knee; i++) {
                double logFreq = powerMin + i / powerRange;
                if (logFreq < logBassPeakAt) {
                    logFreq = logBassPeakAt;
                }
                double clampedIndex = (logFreq - powerMin) * powerRange;
                curve[i] = (float)(rise * (knee - clampedIndex) * positioner + gain);
            }
            return curve;
        }

        /// <inheritdoc/>
        public override void FromBase64(string source) {
            string data = FromBase64String(source);
            string[] parts = data.Split('|');
            if (parts.Length != 5 || parts[0] != nameof(RoomCurveLikeCurve)) {
                throw new FormatException("Invalid RoomCurveLikeCurve data.");
            }
            rise = float.Parse(parts[1]);
            kneeFrequency = double.Parse(parts[2]);
            logBassPeakAt = Math.Log10(double.Parse(parts[3]));
            trebleSuppression = float.Parse(parts[4]);
            kneePosition = Math.Log10(kneeFrequency);
            singleFreqHelper = rise / (Math.Log10(kneeFrequency) - logBassPeakAt);
        }

        /// <inheritdoc/>
        public override string ToBase64() => ToBase64String($"{nameof(RoomCurveLikeCurve)}|{rise}|{kneeFrequency}|{BassPeakAt}|{trebleSuppression}");

        /// <summary>
        /// Default room curve knee frequency.
        /// </summary>
        internal static readonly double defaultKnee = 200;
    }
}
