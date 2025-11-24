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
    }
}
