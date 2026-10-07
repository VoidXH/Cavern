using System;

namespace Cavern.QuickEQ.Crossover.Enums {
    /// <summary>
    /// Extension methods for <see cref="CrossoverSlope"/>.
    /// </summary>
    public static class CrossoverSlopeExtensions {
        /// <summary>
        /// Get the Q-factor for a 2nd-order section of the specified slope type.
        /// </summary>
        /// <param name="slope">The crossover slope type.</param>
        /// <returns>Q-factor for a single biquad stage.</returns>
        public static double GetQFactor(this CrossoverSlope slope) {
            return slope switch {
                CrossoverSlope.Butterworth => 0.7071067811865475, // 1/√2
                CrossoverSlope.LinkwitzRiley => 0.5,
                CrossoverSlope.Bessel => 0.5773502691896258, // 1/√3
                _ => throw new ArgumentOutOfRangeException(nameof(slope), slope, "Unknown crossover slope type")
            };
        }
    }
}
