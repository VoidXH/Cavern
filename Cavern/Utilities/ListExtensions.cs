using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Extra list handling functions.
    /// </summary>
    public static class ListExtensions {
        /// <summary>
        /// Add the item to the list while keeping order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddSorted<T>(this List<T> source, T value) where T : IComparable<T> {
            int index = source.BinarySearch(value);
            if (index < 0) {
                index = ~index;
            }
            source.Insert(index, value);
        }

        /// <summary>
        /// Add the item to the list while keeping order and not allowing duplicates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddSortedDistinct<T>(this List<T> source, T value) where T : IComparable<T> {
            int index = source.BinarySearch(value);
            if (index < 0) {
                source.Insert(~index, value);
            }
        }

        /// <summary>
        /// Remove an item from a sorted <see cref="List{T}"/>.
        /// </summary>
        public static void RemoveSorted<T>(this List<T> source, T value) where T : IComparable<T> {
            int index = source.BinarySearch(value);
            if (index >= 0) {
                source.RemoveAt(index);
            }
        }
    }
}