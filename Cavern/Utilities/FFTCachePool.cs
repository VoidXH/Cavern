using System;
using System.Collections.Generic;

using Cavern.Utilities.Threading;
using Cavern.Waveforms;

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
        /// Fast Fourier transform many 2D <paramref name="signals"/>.
        /// </summary>
        public static MultichannelTransferFunction FFT(MultichannelTransferFunction signals, bool multithreaded) {
            if (signals.Channels == 0) {
                return new MultichannelTransferFunction(0, 0);
            }

            using FFTCachePool pool = new FFTCachePool(signals.Length);
            return pool.FFTAll(signals, multithreaded);
        }

        /// <summary>
        /// Fast Fourier transform many 2D <paramref name="signals"/>.
        /// </summary>
        public static MultichannelTransferFunction FFT(MultichannelWaveform signals, bool multithreaded) {
            if (signals.Channels == 0) {
                return new MultichannelTransferFunction(0, 0);
            }

            using FFTCachePool pool = new FFTCachePool(signals.Length);
            return pool.FFTAll(signals, multithreaded);
        }

        /// <summary>
        /// Perform an operation (channel signal, index, cache for operation) on all <paramref name="signals"/> without checking for exceptions.
        /// </summary>
        public static void ForAllUnchecked(MultichannelWaveform signals, Action<float[], int, FFTCache> operation, bool multithreaded) {
            using FFTCachePool pool = new FFTCachePool(signals.Length);
            Parallelizer.ForUnchecked(0, signals.Channels, i => {
                FFTCache cache = pool.Lease();
                operation(signals[i], i, cache);
                pool.Return(cache);
            }, multithreaded);
        }

        /// <summary>
        /// Perform an operation (channel transfer function, index, cache for operation) on all <paramref name="transferFunctions"/> without checking for exceptions.
        /// </summary>
        public static void ForAllUnchecked(MultichannelTransferFunction transferFunctions, Action<Complex[], int, FFTCache> operation, bool multithreaded) {
            using FFTCachePool pool = new FFTCachePool(transferFunctions.Length);
            Parallelizer.ForUnchecked(0, transferFunctions.Channels, i => {
                FFTCache cache = pool.Lease();
                operation(transferFunctions[i], i, cache);
                pool.Return(cache);
            }, multithreaded);
        }

        /// <summary>
        /// Fast Fourier transform many 2D <paramref name="signals"/>.
        /// </summary>
        public MultichannelTransferFunction FFTAll(MultichannelTransferFunction signals, bool multithreaded) {
            MultichannelTransferFunction result = (MultichannelTransferFunction)signals.Clone();
            FFTAllInPlace(result, multithreaded);
            return result;
        }

        /// <summary>
        /// Fast Fourier transform many 2D <paramref name="signals"/>.
        /// </summary>
        public MultichannelTransferFunction FFTAll(MultichannelWaveform signals, bool multithreaded) {
            Complex[][] result = new Complex[signals.Channels][];
            ForAllUnchecked(signals, (signal, i, cache) => result[i] = signal.FFT(cache), multithreaded);
            return new MultichannelTransferFunction(result);
        }

        /// <summary>
        /// Perform in-place FFT on all the passed <paramref name="signals"/>.
        /// </summary>
        public void FFTAllInPlace(MultichannelTransferFunction signals, bool multithreaded) {
            Parallelizer.ForUnchecked(0, signals.Channels, i => {
                FFTCache cache = Lease();
                signals[i].InPlaceFFT(cache);
                Return(cache);
            }, multithreaded);
        }

        /// <summary>
        /// Perform in-place IFFT on all the passed <paramref name="transferFunctions"/>.
        /// </summary>
        public void IFFTAllInPlace(MultichannelTransferFunction transferFunctions, bool multithreaded) {
            Parallelizer.ForUnchecked(0, transferFunctions.Channels, i => {
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
