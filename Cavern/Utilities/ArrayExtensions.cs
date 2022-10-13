using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Extra array handling functions.
    /// </summary>
    public static class ArrayExtensions {
        /// <summary>
        /// Shorthand for clearing the entire array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this T[] target) => Array.Clear(target, 0, target.Length);

        /// <summary>
        /// Quickly checks if a value is in an array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this float[] target, float value) {
            for (int entry = 0; entry < target.Length; ++entry) {
                if (target[entry] == value) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Shorthand for copying the entire array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[] source, T[] target) => Array.Copy(source, target, source.Length);

        /// <summary>
        /// Clones an array about twice as fast as <see cref="Array.Clone"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FastClone<T>(this T[] source) {
            T[] clone = new T[source.Length];
            Array.Copy(source, clone, source.Length);
            return clone;
        }

        /// <summary>
        /// Checks if an array has any values matching a <paramref name="predicate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this T[] source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Length; ++i) {
                if (predicate(source[i])) {
                    return true;
                }
            }
            return false;
        }
    }
}