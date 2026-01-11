using System;
using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// When performing a large amount of FFTs across multiple threads, use this pool for optimizing allocation performance
    /// by reusing caches that are not used anymore by their thread.
    /// </summary>
    public class FFTCachePool : IDisposable {
        /// <summary>
        /// Size of the used FFTs.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Caches not currently leased.
        /// </summary>
        readonly Stack<FFTCache> caches = new Stack<FFTCache>();

        /// <summary>
        /// Used for safe locking.
        /// </summary>
        readonly object locker = new object();

        /// <summary>
        /// Create an <see cref="FFTCache"/> pool for this FFT size.
        /// </summary>
        public FFTCachePool(int size) => Size = size;

        /// <summary>
        /// Perform in-place FFT on all the passed <paramref name="signals"/>.
        /// </summary>
        public void FFTAllInPlace(Complex[][] signals, bool multithreaded) {
            Parallelizer.ForUnchecked(0, signals.Length, i => {
                FFTCache cache = Lease();
                signals[i].InPlaceFFT(cache);
                Return(cache);
            }, multithreaded);
        }

        /// <summary>
        /// Perform in-place IFFT on all the passed <paramref name="transferFunctions"/>.
        /// </summary>
        public void IFFTAllInPlace(Complex[][] transferFunctions, bool multithreaded) {
            Parallelizer.ForUnchecked(0, transferFunctions.Length, i => {
                FFTCache cache = Lease();
                transferFunctions[i].InPlaceIFFT(cache);
                Return(cache);
            }, multithreaded);
        }

        /// <summary>
        /// Get an <see cref="FFTCache"/> to work with.
        /// </summary>
        public FFTCache Lease() {
            lock (locker) {
                if (caches.Count == 0) {
                    return new ThreadSafeFFTCache(Size);
                } else {
                    return caches.Pop();
                }
            }
        }

        /// <summary>
        /// Store the <paramref name="cache"/> for later reuse.
        /// </summary>
        public void Return(FFTCache cache) {
            lock (locker) {
                caches.Push(cache);
            }
        }

        /// <summary>
        /// Free all resources used by the allocated <see cref="caches"/>.
        /// </summary>
        public void Dispose() {
            while (caches.Count != 0) {
                caches.Pop().Dispose();
            }
        }
    }
}