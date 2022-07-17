using System.Collections.Generic;

namespace Cavern.Filters {
    /// <summary>
    /// Multiple filters in series.
    /// </summary>
    public class ComplexFilter : Filter {
        /// <summary>
        /// Filters to apply on the output.
        /// </summary>
        public readonly List<Filter> Filters = new List<Filter>();

        /// <summary>
        /// Construct an empty filter set.
        /// </summary>
        public ComplexFilter() { }

        /// <summary>
        /// Create a usable filter set from a precreated collection.
        /// </summary>
        public ComplexFilter(params Filter[] filters) => Filters.AddRange(filters);

        /// <summary>
        /// Apply these filters on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            for (int filter = 0, filters = Filters.Count; filter < filters; ++filter) {
                Filters[filter].Process(samples);
            }
        }

        /// <summary>
        /// Apply these filters on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            for (int filter = 0, filters = Filters.Count; filter < filters; ++filter) {
                Filters[filter].Process(samples, channel, channels);
            }
        }
    }
}
