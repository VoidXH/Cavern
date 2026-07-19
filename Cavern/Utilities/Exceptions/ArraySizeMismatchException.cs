using System;

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
    }
}
