namespace Cavern.Filters {
    /// <summary>Abstract audio filter.</summary>
    public abstract class Filter {
        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        public virtual void Process(float[] samples) => Process(samples, 0, 1);

        /// <summary>Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.</summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public abstract void Process(float[] samples, int channel, int channels);
    }
}