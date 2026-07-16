using System;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a sync word check has failed.
    /// </summary>
    public class SyncException : Exception {
        const string message = "Sync word check has failed. The bitstream is damaged, invalid, or detected as a wrong codec.";

        /// <summary>
        /// Tells if a sync word check has failed.
        /// </summary>
        public SyncException() : base(message) { }
    }
}
