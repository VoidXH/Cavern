using System;

namespace Cavern.Common {
    /// <summary>
    /// A jslib function ran to an exception.
    /// </summary>
    public class JSlibException : Exception {
        const string message = "A JavaScript library has thrown an exception, which was logged to the browser console.";

        /// <summary>
        /// A jslib function ran to an exception.
        /// </summary>
        public JSlibException() : base(message) { }
    }

    /// <summary>
    /// A jslib file was either missing or corrupt.
    /// </summary>
    public class JSlibNotFoundException : Exception {
        const string message = "The plugin file {0} or a part of its code is missing. Please include the latest from the Cavern API.";

        /// <summary>
        /// A jslib file was either missing or corrupt.
        /// </summary>
        public JSlibNotFoundException(string filename) : base(string.Format(message, filename)) { }
    }

    /// <summary>
    /// The permission for accessing the resource was denied by the user.
    /// </summary>
    public class PermissionDeniedException : Exception {
        const string message = "The permission for accessing the resource was denied by the user.";

        /// <summary>
        /// The permission for accessing the resource was denied by the user.
        /// </summary>
        public PermissionDeniedException() : base(message) { }
    }
}