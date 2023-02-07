using System;

namespace CavernizeGUI {
    /// <summary>
    /// Tells that an operation is already running.
    /// </summary>
    public class ConcurrencyException : Exception {
        /// <summary>
        /// Tells that an operation is already running.
        /// </summary>
        public ConcurrencyException(string message) : base(message) { }
    }

    /// <summary>
    /// Tells that some applied settings are not compatible with each other.
    /// </summary>
    public class IncompatibleSettingsException : Exception {
        /// <summary>
        /// Tells that some applied settings are not compatible with each other.
        /// </summary>
        public IncompatibleSettingsException(string message) : base(message) { }
    }

    /// <summary>
    /// Tells that a track setup is wrong or unsupported.
    /// </summary>
    public class TrackException : Exception {
        /// <summary>
        /// Tells that a track setup is wrong or unsupported.
        /// </summary>
        public TrackException(string message) : base(message) { }
    }
}