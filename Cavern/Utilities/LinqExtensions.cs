using System;
using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// Operations that LINQ should have had.
    /// </summary>
    public static class LinqExtensions {
        /// <summary>
        /// Get the index of a given <paramref name="item"/> in a <paramref name="source"/> enumerable or -1 if it's not in the collection.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, T item) {
            int index = 0;
            foreach (T element in source) {
                if (EqualityComparer<T>.Default.Equals(element, item)) {
                    return index;
                }
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first element in the sequence that satisfies the specified condition.
        /// </summary>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to search through</param>
        /// <param name="query">A predicate function to test each element for a condition</param>
        /// <returns>The index of the first element that passes the test in the sequence, or -1 if no such element is found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> query) {
            int index = 0;
            foreach (T item in source) {
                if (query(item)) {
                    return index;
                }
                index++;
            }
            return -1;
        }
    }
}
