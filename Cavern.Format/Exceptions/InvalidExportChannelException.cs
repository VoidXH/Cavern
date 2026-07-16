using System;

using Cavern.Channels;
using Cavern.Format.Common;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if some channels are not supported by the export format.
    /// </summary>
    public class InvalidExportChannelException : Exception {
        const string message = "Some channels ({0}) are not supported by the export format.";

        /// <summary>
        /// With <see cref="CavernFormatGlobal.Unsafe"/>, this limitation can be disabled. Doing that would cause spatial placement issues, but allows export.
        /// </summary>
        public bool Bypassable { get; }

        /// <summary>
        /// Channels from the operation that are not supported by the export format.
        /// </summary>
        public ReferenceChannel[] Channels { get; }

        /// <summary>
        /// Tells if some <paramref name="channels"/> are not supported by the export format.
        /// </summary>
        public InvalidExportChannelException(bool bypassable, params ReferenceChannel[] channels) : base(string.Format(message, string.Join(", ", channels))) {
            Bypassable = bypassable;
            Channels = channels;
        }
    }
}
