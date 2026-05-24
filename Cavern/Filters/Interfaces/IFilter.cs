namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// Audio block processing filter.
    /// </summary>
    public interface IFilter {
        /// <summary>
        /// <see cref="Process(float[])"/>ing a Dirac-delta will result in an impulse response that will result in the same exact filter
        /// when used as convolution samples.
        /// </summary>
        bool LinearTimeInvariant => true;

        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        void Process(float[] samples);

        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        void Process(float[] samples, int channel, int channels);
    }
}
