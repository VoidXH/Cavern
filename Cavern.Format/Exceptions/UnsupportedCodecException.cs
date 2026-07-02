using System;

using Cavern.Format.Common;

namespace Cavern.Format.Exceptions {
    /// <summary>
    /// Tells if a codec is unsupported.
    /// </summary>
    public class UnsupportedCodecException : Exception {
        const string message = "Unsupported {0}codec: {1}.";
        const string audio = "audio ";
        const string video = "video ";

        /// <summary>
        /// Tells if a codec is unsupported.
        /// </summary>
        public UnsupportedCodecException(Codec codec) : base(string.Format(message, codec.IsAudio() ? audio : video, codec)) { }

        /// <summary>
        /// Tells if a codec is unsupported.
        /// </summary>
        public UnsupportedCodecException(string name) : base(string.Format(message, string.Empty, name)) { }
    }
}
