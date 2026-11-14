using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities.Exceptions {
    /// <summary>
    /// Tells if two arrays should have equal size, but they don't.
    /// </summary>
    public class ArraySizeMismatchException : Exception {
        const string message = "The array size of {0} and {1} doesn't match.";

        /// <summary>
        /// Tells if two arrays should have equal size, but they don't.
        /// </summary>
        public ArraySizeMismatchException(string array1, string array2) : base(string.Format(message, array1, array2)) { }

        /// <summary>
        /// Tells if two arrays should have equal size, but they don't.
        /// </summary>
        public ArraySizeMismatchException(int size1, int size2) : base(string.Format(message, size1, size2)) { }

        /// <summary>
        /// Tells if two arrays should have equal size, but they don't.
        /// </summary>
        public ArraySizeMismatchException(long size1, long size2) : base(string.Format(message, size1, size2)) { }

        /// <summary>
        /// Throw the exception if the sizes of the arrays don't match.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotEqual<T>(T[] size1, T[] size2) {
            if (size1.LongLength != size2.LongLength) {
                throw new ArraySizeMismatchException(size1.LongLength, size2.LongLength);
            }
        }

        /// <summary>
        /// Throw the exception if the sizes of the arrays don't match.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotEqual<T>(IList<T> size1, IList<T> size2) {
            if (size1.Count != size2.Count) {
                throw new ArraySizeMismatchException(size1.Count, size2.Count);
            }
        }
    }
}
