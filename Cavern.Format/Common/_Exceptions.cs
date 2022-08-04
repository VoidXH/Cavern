using System;

namespace Cavern.Format.Common {
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

    /// <summary>
    /// Tells if the decoded stream is corrupted.
    /// </summary>
    public class CorruptionException : Exception {
        const string message = "The decoder found corrupted data at {0}.";

        /// <summary>
        /// Tells if the decoded stream is corrupted.
        /// </summary>
        public CorruptionException(string location) : base(string.Format(message, location)) { }
    }

    /// <summary>
    /// Tells if the decoder ran into a predefined error code that is found in the decoder's documentation.
    /// </summary>
    public class DecoderException : Exception {
        const string message = "Internal decoder error code: {0}.";

        /// <summary>
        /// Tells if the decoder ran into a predefined error code that is found in the decoder's documentation.
        /// </summary>
        public DecoderException(int errorCode) : base(string.Format(message, errorCode)) { }
    }

    /// <summary>
    /// Tells that a decoder which can process an infinite stream is not able to return content length.
    /// </summary>
    public class RealtimeLengthException : Exception {
        const string message = "This is an infinite decoder. Content lenth is not readable from the bitstream.";

        /// <summary>
        /// Tells that a decoder which can process an infinite stream is not able to return content length.
        /// </summary>
        public RealtimeLengthException() : base(message) { }
    }

    /// <summary>
    /// Tells if a decoded value is reserved. This could mark a transport error.
    /// </summary>
    public class ReservedValueException : Exception {
        const string message = "A reserved value of {0} was found in the stream. " +
            "This error most likely means that the file is corrupt.";

        /// <summary>
        /// Tells if a decoded value is reserved. This could mark a transport error.
        /// </summary>
        public ReservedValueException(string feature) : base(string.Format(message, feature)) { }
    }

    /// <summary>
    /// Tells is a feature is only available when reading from a raw file.
    /// </summary>
    public class StreamingException : Exception {
        const string message = "This stream is read from a container or other wrapper. " +
            "The operation you tried to perfrom should be done on the parent.";

        /// <summary>
        /// Tells is a feature is only available when reading from a raw file.
        /// </summary>
        public StreamingException() : base(message) { }
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
        const string message = "Unsupported {0} codec: {1}.",
            audio = "audio",
            video = "video";

        /// <summary>
        /// Tells if a codec is unsupported.
        /// </summary>
        public UnsupportedCodecException(bool needAudio, Codec codec) :
            base(string.Format(message, needAudio ? audio : video, codec)) { }
    }

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

    /// <summary>
    /// Tells if no supported file format was detected.
    /// </summary>
    public class UnsupportedFormatException : Exception {
        const string message = "No supported file format was detected.";

        /// <summary>
        /// Tells if no supported file format was detected.
        /// </summary>
        public UnsupportedFormatException() : base(message) { }
    }
}