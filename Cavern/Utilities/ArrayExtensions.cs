using System;
using System.Linq;
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
        /// Quickly checks if a value is in an array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Value to check</param>
        /// <returns>If an array contains the value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this T[] target, T value) where T : struct {
            for (int entry = 0; entry < target.Length; ++entry) {
                if (target[entry].Equals(value)) {
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
        /// Deep copies a 1-dimensional array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] DeepCopy1D<T>(this T[] source) where T : ICloneable {
            T[] clone = source.FastClone();
            for (int i = 0; i < clone.Length; i++) {
                clone[i] = (T)clone[i].Clone();
            }
            return clone;
        }

        /// <summary>
        /// Deep copies a 2-dimensional array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[][] DeepCopy2D<T>(this T[][] source) {
            T[][] clone = source.FastClone();
            for (int i = 0; i < clone.Length; i++) {
                clone[i] = clone[i].FastClone();
            }
            return clone;
        }

        /// <summary>
        /// Checks if an array has any values matching a <paramref name="predicate"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this T[] source, Func<T, bool> predicate) {
            for (int i = 0; i < source.Length; i++) {
                if (predicate(source[i])) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if an array is the <paramref name="subset"/> of the <paramref name="source"/>.
        /// </summary>
        public static bool IsSubsetOf<T>(this T[] source, T[] subset) {
            for (int i = 0; i < subset.Length; i++) {
                if (!source.Contains(subset[i])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// From a <paramref name="source"/>, get which element is closest to a given <paramref name="value"/>.
        /// </summary>
        public static float Nearest(this float[] source, float value) {
            int left = 0, right = source.Length - 1;
            float closest = source[0];
            while (left <= right) {
                int mid = (left + right) / 2;
                if (source[mid] == value) {
                    return source[mid];
                }
                if (Math.Abs(source[mid] - value) < Math.Abs(closest - value)) {
                    closest = source[mid];
                }
                if (source[mid] < value) {
                    left = mid + 1;
                } else {
                    right = mid - 1;
                }
            }
            return closest;
        }

        /// <summary>
        /// Remove the 0 or default elements from the end of an array.
        /// </summary>
        public static void RemoveZeros<T>(ref T[] arr) where T : IComparable {
            int newLength = arr.Length;
            while (newLength > 0) {
                if (arr[--newLength].CompareTo(default(T)) != 0) {
                    newLength++;
                    break;
                }
            }
            Array.Resize(ref arr, newLength);
        }
    }
}