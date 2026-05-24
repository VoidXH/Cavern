using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Creates <see cref="Filter"/> instances depending on if the filter was initialized for CavernAmp.
    /// </summary>
    public static class FilterFactory {
        /// <summary>
        /// Creates a gain filter with the specified <paramref name="gain"/> value in decibels.
        /// </summary>
        public static IGainFilter CreateGain(double gain) => CavernAmp.Available ? (IGainFilter)new GainAmp(gain) : new Gain(gain);
    }
}
