using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Data that feeds the <see cref="PhaseRenderer"/>.
    /// </summary>
    public readonly struct PhaseRendererTransferEntry {
        /// <summary>
        /// Transfer function of the measured channel.
        /// </summary>
        public readonly Complex[] transferFunction;

        /// <summary>
        /// The <see cref="transferFunction"/> is valid until this frequency - cutoff or the speaker's limit.
        /// </summary>
        public readonly float endFrequency;

        /// <summary>
        /// Display the curve with this color on the graph.
        /// </summary>
        public readonly uint color;

        /// <summary>
        /// Data that feeds the <see cref="PhaseRenderer"/>.
        /// </summary>
        /// <param name="transferFunction">Transfer function of the measured channel</param>
        /// <param name="endFrequency">The <paramref name="transferFunction"/> is valid until this frequency - cutoff or the speaker's limit</param>
        /// <param name="color">Display the curve with this color on the graph</param>
        public PhaseRendererTransferEntry(Complex[] transferFunction, float endFrequency, uint color) {
            this.transferFunction = transferFunction;
            this.endFrequency = endFrequency;
            this.color = color;
        }

        /// <summary>
        /// Data that feeds the <see cref="PhaseRenderer"/>.
        /// </summary>
        /// <param name="entry">Convert this impulse response-based entry to a transfer function-based one</param>
        /// <param name="cache">FFT optimization cache</param>
        public PhaseRendererTransferEntry(PhaseRendererImpulseEntry entry, FFTCache cache) {
            transferFunction = Measurements.FFT(entry.impulseResponse, cache);
            endFrequency = entry.endFrequency;
            color = entry.color;
        }
    }
}
