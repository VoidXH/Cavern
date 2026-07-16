using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if the container can't be determined by file type.
    /// </summary>
    public class UnsupportedContainerForWriteException : Exception {
        const string message = "The {0} container is not supported for writing.";

        /// <summary>
        /// Tells if the container can't be determined by file type.
        /// </summary>
        public UnsupportedContainerForWriteException(string fileType) : base(string.Format(message, fileType)) { }
    }
}
