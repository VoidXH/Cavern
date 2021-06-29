using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Extra array handling functions.</summary>
    public static class ArrayExtensions {
        /// <summary>Clones an array about twice as fast as <see cref="Array.Clone"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FastClone<T>(this T[] source) {
            T[] clone = new T[source.Length];
            Array.Copy(source, clone, source.Length);
            return clone;
        }
    }
}
