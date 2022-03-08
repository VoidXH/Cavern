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

    /// <summary>
    /// Tells if a codec is unsupported.
    /// </summary>
    public class UnsupportedCodecException : Exception {
        const string message = "Unsupported {0} codec: {1}",
            audio = "audio",
            video = "video";

        /// <summary>
        /// Tells if a codec is unsupported.
        /// </summary>
        public UnsupportedCodecException(bool needAudio, Codec codec) :
            base(string.Format(message, needAudio ? audio : video, codec.ToString())) { }
    }
}