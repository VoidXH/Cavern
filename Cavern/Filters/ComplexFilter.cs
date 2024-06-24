using System.Collections.Generic;
using System.Linq;

namespace Cavern.Filters {
    /// <summary>
    /// Multiple filters in series.
    /// </summary>
    public class ComplexFilter : Filter {
        /// <inheritdoc/>
        public override bool LinearTimeInvariant => Filters.All(x => x.LinearTimeInvariant);

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

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            for (int filter = 0, filters = Filters.Count; filter < filters; ++filter) {
                Filters[filter].Process(samples);
            }
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            for (int filter = 0, filters = Filters.Count; filter < filters; ++filter) {
                Filters[filter].Process(samples, channel, channels);
            }
        }

        /// <inheritdoc/>
        public override object Clone() {
            ComplexFilter result = new ComplexFilter();
            result.Filters.AddRange(Filters.Select(x => (Filter)x.Clone()));
            return result;
        }
    }
}
