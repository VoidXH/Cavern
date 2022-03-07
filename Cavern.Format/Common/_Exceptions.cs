using System;

namespace Cavern.Format.Common {
    /// <summary>
    /// Tells if the selected track had an invalid ID.
    /// </summary>
    public class InvalidTrackException : Exception {
        const string message = "Invalid track: {0} of {1}";

        /// <summary>
        /// Tells if the selected track had an invalid ID.
        /// </summary>
        public InvalidTrackException(int id, int tracks) : base(string.Format(message, id, tracks)) { }
    }

    public class UnsupportedCodecException : Exception {
        const string message = "Unsupported {0} codec: {1}",
            audio = "audio",
            video = "video";

        public UnsupportedCodecException(bool needAudio, Codec codec) :
            base(string.Format(message, needAudio ? audio : video, codec.ToString())) { }
    }
}