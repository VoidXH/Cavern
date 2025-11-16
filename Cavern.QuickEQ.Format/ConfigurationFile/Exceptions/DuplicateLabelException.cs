using System;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Thrown when a format that doesn't allow duplicate labels would be used to export duplicate labels.
    /// </summary>
    public class DuplicateLabelException : Exception {
        const string message = "This format expects all labels to differ. \"{0}\" was present more than once.";

        /// <summary>
        /// Thrown when a format that doesn't allow duplicate labels would be used to export duplicate labels.
        /// </summary>
        public DuplicateLabelException(string label) : base(string.Format(message, label)) { }
    }
}
