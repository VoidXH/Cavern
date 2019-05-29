using System.Collections.Generic;

namespace Cavern.Filters {
    /// <summary>Multiple filters in series.</summary>
    public class ComplexFilter : Filter {
        /// <summary>Filters to apply on the output.</summary>
        public readonly List<Filter> Filters = new List<Filter>();

        /// <summary>Apply these filters on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public override void Process(float[] Samples) {
            for (int i = 0, c = Filters.Count; i < c; ++i)
                Filters[i].Process(Samples);
        }

        /// <summary>Apply these filters on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public override void Process(float[] Samples, int Channel, int Channels) {
            for (int i = 0, c = Filters.Count; i < c; ++i)
                Filters[i].Process(Samples, Channel, Channels);
        }
    }
}
