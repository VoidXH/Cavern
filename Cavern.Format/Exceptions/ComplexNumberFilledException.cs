using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if an operation can only handle complex numbers of which only a single component is set.
    /// </summary>
    public class ComplexNumberFilledException : Exception {
        const string message = "This operation can only handle complex numbers of which only a single component is set.";

        /// <summary>
        /// Tells if an operation can only handle complex numbers of which only a single component is set.
        /// </summary>
        public ComplexNumberFilledException() : base(message) { }
    }
}
