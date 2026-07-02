using System;

namespace Cavern.Format.Networking.Exceptions {
    /// <summary>
    /// Parsing a packet has failed due to invalid data or structure.
    /// </summary>
    public class InvalidPacketException : Exception {
        /// <summary>
        /// Parsing a packet has failed due to invalid data or structure.
        /// </summary>
        public InvalidPacketException(string message) : base(message) { }
    }
}
