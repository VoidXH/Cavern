using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP DDRC-88A.
    /// </summary>
    /// <remarks>MiniDSP DDRC-88A only works on 48 kHz sampling rate. Using anything else breaks the filter set.</remarks>
    public class MiniDSPDDRC88AFilterSet : MiniDSP2x4HDFilterSetLite {
        /// <summary>
        /// IIR filter set for MiniDSP DDRC-88A.
        /// </summary>
        public MiniDSPDDRC88AFilterSet(int channels) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given set of channels.
        /// </summary>
        public MiniDSPDDRC88AFilterSet(ReferenceChannel[] channels) : base(channels, sampleRate) { }

        /// <summary>
        /// Fixed sample rate of the DDRC-88A.
        /// </summary>
        const int sampleRate = 48000;
    }
}