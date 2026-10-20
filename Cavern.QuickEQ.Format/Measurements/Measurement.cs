using Cavern.QuickEQ.Measurement;

namespace Cavern.Format.Measurements {
    /// <summary>
    /// Contains a multiploint measurement's data.
    /// </summary>
    public abstract class Measurement {
        /// <summary>
        /// Sample rate of the measurement data.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Measurements grouped by microphone position, in the order the positions appear in the file.
        /// </summary>
        public MeasuredPosition[] Positions { get; protected set; }
    }
}
