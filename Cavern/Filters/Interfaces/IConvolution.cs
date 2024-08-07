﻿namespace Cavern.Filters.Interfaces {
    /// <summary>
    /// A filter that's implementing a convolution with a single impulse response using any algorithm.
    /// </summary>
    public interface IConvolution : ISampleRateDependentFilter {
        /// <summary>
        /// Impulse response to convolve with.
        /// </summary>
        float[] Impulse { get; set; }

        /// <summary>
        /// Added delay to the filter's <see cref="Impulse"/>, in samples.
        /// </summary>
        int Delay { get; set; }
    }
}