namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// A filter that's implementing a convolution with a single impulse response using any algorithm.
    /// </summary>
    public interface IConvolution : ISampleRateDependentFilter {
        /// <summary>
        /// Impulse response to convolve with.
        /// </summary>
        float[] Impulse { get; set; }
    }
}