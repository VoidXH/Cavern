using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a decoded value is reserved. This could mark a transport error.
    /// </summary>
    public class ReservedValueException : Exception {
        const string message = "A reserved value of {0} was found in the stream. This error most likely means that the file is corrupt.";

        /// <summary>
        /// Tells if a decoded value is reserved. This could mark a transport error.
        /// </summary>
        public ReservedValueException(string feature) : base(string.Format(message, feature)) { }
    }
}
