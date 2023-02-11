using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Performs an optimized convolution while being thread-safe.
    /// </summary>
    /// <remarks>This filter is using the overlap and add method using FFTs.</remarks>
    public class ThreadSafeFastConvolver : FastConvolver {
        /// <summary>
        /// Constructs a thread-safe optimized convolution with no delay.
        /// </summary>
        public ThreadSafeFastConvolver(float[] impulse) : base(impulse) { }

        /// <summary>
        /// Constructs a thread-safe optimized convolution with added delay.
        /// </summary>
        public ThreadSafeFastConvolver(float[] impulse, int delay) : base(impulse, delay) { }

        /// <summary>
        /// Create the FFT cache used for accelerating the convolution in Fourier-space.
        /// </summary>
        public override FFTCache CreateCache(int fftSize) => new ThreadSafeFFTCache(fftSize);
    }
}