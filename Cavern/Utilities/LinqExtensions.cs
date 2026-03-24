using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Make <paramref name="n"/>-1 copies of a specific <paramref name="item"/>, with the first item in the resulting array being the original copy.
        /// </summary>
        public static T[] Multiply<T>(this T item, int n) where T : ICloneable {
            T[] result = new T[n];
            result[0] = item;
            for (int i = 1; i < n; i++) {
                result[i] = (T)item.Clone();
            }
            return result;
        }

        /// <summary>
        /// Make <paramref name="n"/>-1 copies of a specific <paramref name="item"/>.
        /// </summary>
        public static T[] MultiplyStruct<T>(this T item, int n) where T : struct {
            T[] result = new T[n];
            result[0] = item;
            for (int i = 1; i < n; i++) {
                result[i] = item;
            }
            return result;
        }

        /// <summary>
        /// Shorthand for .Select(...).ToArray(), with heavy performance optimizations.
        /// </summary>
        public static T2[] SelectArray<T1, T2>(this IEnumerable<T1> source, Func<T1, T2> selector) {
            if (source is IList<T1> list) { // Fast path for indexable structures
                T2[] result = new T2[list.Count];
                for (int i = 0; i < list.Count; i++) {
                    result[i] = selector(list[i]);
                }
                return result;
            } else if (source is ICollection<T1> collection) { // Fast path for countable but not indexable structures
                T2[] result = new T2[collection.Count];
                int i = 0;
                IEnumerator<T1> enumerator = collection.GetEnumerator();
                while (enumerator.MoveNext()) {
                    result[i++] = selector(enumerator.Current);
                }
                return result;
            } else { // Slow path for structures not supporting .Count
                return source.Select(selector).ToArray();
            }
        }
    }
}
