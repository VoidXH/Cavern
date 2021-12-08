using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Extra array handling functions.
    /// </summary>
    public static class ArrayExtensions {
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
            for (int i = 0; i < source.Length; ++i)
                if (predicate(source[i]))
                    return true;
            return false;
        }
    }
}