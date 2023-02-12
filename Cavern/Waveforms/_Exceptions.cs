using System;

namespace Cavern.Waveforms {
    /// <summary>
    /// Tells if a multichannel waveform's channels wouldn't have a matching length.
    /// </summary>
    public class DifferentSignalLengthsException : Exception {
        const string message = "The sample count of the channels doesn't match.";

        /// <summary>
        /// Tells if a multichannel waveform's channels wouldn't have a matching length.
        /// </summary>
        public DifferentSignalLengthsException() : base(message) { }
    }
}