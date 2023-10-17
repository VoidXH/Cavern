﻿using System;
using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// When performing a large amount of FFTs across multiple threads, use this pool for optimizing allocation performance
    /// by reusing caches that are not used anymore by their thread.
    /// </summary>
    public class FFTCachePool : IDisposable {
        /// <summary>
        /// Caches not currently leased.
        /// </summary>
        readonly Stack<FFTCache> caches = new Stack<FFTCache>();

        /// <summary>
        /// Size of the used FFTs.
        /// </summary>
        readonly int size;

        /// <summary>
        /// Create an <see cref="FFTCache"/> pool for this FFT size.
        /// </summary>
        public FFTCachePool(int size) => this.size = size;

        /// <summary>
        /// Get an <see cref="FFTCache"/> to work with.
        /// </summary>
        public FFTCache Lease() {
            lock (this) {
                if (caches.Count == 0) {
                    return new ThreadSafeFFTCache(size);
                } else {
                    return caches.Pop();
                }
            }
        }

        /// <summary>
        /// Store the <paramref name="cache"/> for later reuse.
        /// </summary>
        public void Return(FFTCache cache) {
            lock (this) {
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