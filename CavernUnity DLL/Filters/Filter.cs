namespace Cavern.Filters {
    /// <summary>Abstract audio filter.</summary>
    public abstract class Filter {
        /// <summary>Apply this filter on a set of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public abstract void Process(float[] Samples);

        /// <summary>Apply this filter on a set of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="Samples">Input samples</param>
        /// <param name="Channel">Channel to filter</param>
        /// <param name="Channels">Total channels</param>
        public abstract void Process(float[] Samples, int Channel, int Channels);
    }
}