using System;

namespace Cavern.Utilities.Exceptions {
    /// <summary>
    /// Thrown when the <see cref="FFTCache"/> used is too small for the requested FFT operation.
    /// </summary>
    public class SmallFFTCacheException : Exception {
        const string message = "The FFT cache size ({0}) is too small for a signal length of {1}.";

        /// <summary>
        /// Thrown when the <see cref="FFTCache"/> used is too small for the requested FFT operation.
        /// </summary>
        public SmallFFTCacheException(int cacheSize, int signalLength) : base(string.Format(message, cacheSize, signalLength)) { }

        /// <summary>
        /// Checks if the FFT size exceeds the cache capacity and throws if it does.
        /// </summary>
        public static void ThrowIfTooSmall(int cacheSize, int signalLength) {
            if (signalLength > cacheSize) {
                throw new SmallFFTCacheException(cacheSize, signalLength);
            }
        }
    }
}
