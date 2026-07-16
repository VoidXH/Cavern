using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a required feature in the codec is unsupported.
    /// </summary>
    public class UnsupportedFeatureException : Exception {
        const string message = "A required feature in the codec is unsupported: {0}.";

        /// <summary>
        /// Tells if a required feature in the codec is unsupported.
        /// </summary>
        public UnsupportedFeatureException(string featureName) : base(string.Format(message, featureName)) { }
    }
}
