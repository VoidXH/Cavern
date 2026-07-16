using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if no supported file format was detected.
    /// </summary>
    public class UnsupportedFormatException : Exception {
        const string message = "No supported file format was detected.";

        const string message2 = " Detected unknown magic number: ";

        const string message2b = " Detected unknown format name: ";

        /// <summary>
        /// Tells if no supported file format was detected.
        /// </summary>
        public UnsupportedFormatException() : base(message) { }

        /// <summary>
        /// Tells if no supported file format was detected, and gives a hint to what the file could be.
        /// </summary>
        public UnsupportedFormatException(int magicNumber) : base(message + message2 + magicNumber.ToString("X8")) { }

        /// <summary>
        /// Tells if no supported file format was detected, and gives a hint to what the file could be.
        /// </summary>
        public UnsupportedFormatException(string name) : base(message + message2b + name) { }
    }
}
