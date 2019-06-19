using Cavern.Filters;

namespace Cavern.QuickEQ {
    /// <summary>Measures properties of a filter, like frequency/impulse response, gain, or delay.</summary>
    public class FilterAnalyzer {
        /// <summary>Filter to measure.</summary>
        readonly Filter filter;
        /// <summary>Sample rate used for measurements and in <see cref="filter"/> if it's sample rate-dependent.</summary>
        readonly int sampleRate;

        /// <summary>Copy a filter for measurements.</summary>
        /// <param name="filter">Filter to measure</param>
        /// <param name="sampleRate">Sample rate used for measurements and in <paramref name="filter"/> if it's sample rate-dependent</param>
        public FilterAnalyzer(Filter filter, int sampleRate) {
            this.filter = filter;
            this.sampleRate = sampleRate;
        }
    }
}