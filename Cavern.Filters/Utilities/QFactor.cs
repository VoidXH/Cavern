using System;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Q-factor conversion utilities.
    /// </summary>
    public static class QFactor {
        /// <summary>
        /// Sqrt(2)/2, the reference Q factor.
        /// </summary>
        public const double reference = .7071067811865475;

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
    }
}