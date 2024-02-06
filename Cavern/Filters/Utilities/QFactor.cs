using System;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Q-factor conversion utilities.
    /// </summary>
    public static class QFactor {
        /// <summary>
        /// Convert bandwidth to Q-factor.
        /// </summary>
        public static double FromBandwidth(double centerFreq, double startFreq, double endFreq) => centerFreq / (endFreq - startFreq);

        /// <summary>
        /// Convert bandwidth to Q-factor.
        /// </summary>
        public static double FromBandwidth(double centerFreq, double freqRange) => centerFreq / freqRange;

        /// <summary>
        /// Convert bandwidth to Q-factor.
        /// </summary>
        public static double FromBandwidth(double octaves) {
            double pow = Math.Pow(2, octaves);
            return Math.Sqrt(pow) / (pow - 1);
        }

        /// <summary>
        /// Convert slope to Q-factor.
        /// </summary>
        /// <param name="slope">Filter steepness factor</param>
        /// <param name="gain">Filter gain in decibels</param>
        public static double FromSlope(double slope, double gain) {
            double a = Math.Pow(10, gain * .05f);
            return 1.0 / Math.Sqrt((a + 1 / a) * (1 / slope - 1) + 2);
        }

        /// <summary>
        /// Convert slope to Q-factor.
        /// </summary>
        /// <param name="slope">Filter steepness in decibels</param>
        /// <param name="gain">Filter gain in decibels</param>
        public static double FromSlopeDecibels(double slope, double gain) {
            double a = Math.Pow(10, gain * .05f);
            return 1.0 / Math.Sqrt((a + 1 / a) * (1 / Math.Abs(slope / gain) - 1) + 2);
        }

        /// <summary>
        /// Convert Q-factor to bandwidth.
        /// </summary>
        public static double ToBandwidth(double qFactor) {
            const double log2 = 0.30102999566398119521373889472449;
            qFactor = 1 / (qFactor * qFactor);
            double num = qFactor + 2;
            return (Math.Log10(1 + qFactor * .5 + Math.Sqrt(num * num * .25 - 1))) / log2;
        }

        /// <summary>
        /// Convert Q-factor to steepness in decibels/octave.
        /// </summary>
        /// <param name="qFactor">Filter Q-factor</param>
        /// <param name="gain">Filter gain in decibels</param>
        public static double ToSlope(double qFactor, double gain) {
            double a = Math.Pow(10, gain * .05f);
            return 1.0 / ((1 / (qFactor * qFactor) - 2) / (a + 1 / a) + 1);
        }

        /// <summary>
        /// Sqrt(2)/2, the Q factor for maximum flatness.
        /// </summary>
        public const double reference = .7071067811865475;
    }
}