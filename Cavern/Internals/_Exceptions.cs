using System;

namespace Cavern.Internals {
    /// <summary>
    /// Tells if the developer used something without properly understanding what it does.
    /// </summary>
    public class DevHasNoIdeaException : Exception {
        const string message = "The developer is very irresponsible.";

        /// <summary>
        /// Tells if the developer used something without properly understanding what it does.
        /// </summary>
        public DevHasNoIdeaException() : base(message) { }
    }
}