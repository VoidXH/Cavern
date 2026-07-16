using System;

using Cavern.Format.Common;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if no stream was present in the container with the selected codec.
    /// </summary>
    public class CodecNotFoundException : Exception {
        const string message = "No stream is present in the container with the selected codec: {0}.";

        /// <summary>
        /// Tells if no stream was present in the container with the selected codec.
        /// </summary>
        public CodecNotFoundException(Codec codec) : base(string.Format(message, codec)) { }
    }
}
