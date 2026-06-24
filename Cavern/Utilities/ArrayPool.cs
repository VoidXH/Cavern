using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// When performing a large amount of array allocation-requiring operations across multiple threads, use this pool for optimizing allocation
    /// performance by reusing caches that are not used anymore by their thread. This is useful for repeatedly performed FFTs for example.
    /// </summary>
    /// <remarks>Because .NET is managed, you don't have to worry about not returning the arrays, worst case scenario is not gaining performance.
    /// But you should use this properly tho.</remarks>
    public class ArrayPool<T> {
        /// <summary>
        /// Size of the used FFTs.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Caches not currently leased.
        /// </summary>
        readonly Stack<T[]> caches = new Stack<T[]>();

        /// <summary>
        /// Initialize an array pool for given <paramref name="size"/>d arrays.
        /// </summary>
        public ArrayPool(int size) => Size = size;

        /// <summary>
        /// Get an array to work with.
        /// </summary>
        public T[] Lease() {
            lock (this) {
                if (caches.Count == 0) {
                    return new T[Size];
                } else {
                    return caches.Pop();
                }
            }
        }

        /// <summary>
        /// Store the <paramref name="cache"/> for later reuse.
        /// </summary>
        public void Return(T[] cache) {
            lock (this) {
                caches.Push(cache);
            }
        }

        /// <summary>
        /// Store the <paramref name="cache"/> for later reuse.
        /// </summary>
        public void Return(T[][] cache) {
            lock (this) {
                for (int i = 0; i < cache.Length; i++) {
                    caches.Push(cache[i]);
                }
            }
        }
    }
}
