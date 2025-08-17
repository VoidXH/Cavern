namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Data that feeds the <see cref="PhaseRenderer"/>.
    /// </summary>
    public readonly struct PhaseRendererImpulseEntry {
        /// <summary>
        /// Impulse response of the measured channel.
        /// </summary>
        public readonly float[] impulseResponse;

        /// <summary>
        /// The <see cref="impulseResponse"/> is valid until this frequency - cutoff or the speaker's limit.
        /// </summary>
        public readonly float endFrequency;

        /// <summary>
        /// Display the curve with this color on the graph.
        /// </summary>
        public readonly uint color;

        /// <summary>
        /// Data that feeds the <see cref="PhaseRenderer"/>.
        /// </summary>
        /// <param name="impulseResponse">Impulse response of the measured channel</param>
        /// <param name="endFrequency">The <paramref name="impulseResponse"/> is valid until this frequency - cutoff or the speaker's limit</param>
        /// <param name="color">Display the curve with this color on the graph</param>
        public PhaseRendererImpulseEntry(float[] impulseResponse, float endFrequency, uint color) {
            this.impulseResponse = impulseResponse;
            this.endFrequency = endFrequency;
            this.color = color;
        }
    }
}
