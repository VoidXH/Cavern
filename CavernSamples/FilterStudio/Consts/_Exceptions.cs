using System;

namespace FilterStudio {
    /// <summary>
    /// Tells if value can't be edited for a filter, because the type is not currently supported for parsing.
    /// </summary>
    public class UneditableTypeException : Exception {
        const string message = "This value can't be edited, because the type is not currently supported for parsing.";

        /// <summary>
        /// Tells if value can't be edited for a filter, because the type is not currently supported for parsing.
        /// </summary>
        public UneditableTypeException() : base(message) { }
    }
}