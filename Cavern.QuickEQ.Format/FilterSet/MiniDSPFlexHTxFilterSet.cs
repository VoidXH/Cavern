using Cavern.Channels;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP Flex HT and HTx.
    /// </summary>
    /// <remarks>MiniDSP Flex HT and HTx only work on 48 kHz sampling rate. Using anything else breaks the filter set.</remarks>
    public class MiniDSPFlexHTxFilterSet : MiniDSP2x4HDFilterSet {
        /// <summary>
        /// IIR filter set for MiniDSP Flex HT and HTx.
        /// </summary>
        public MiniDSPFlexHTxFilterSet(int channels) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP 2x4 with a given set of channels.
        /// </summary>
        public MiniDSPFlexHTxFilterSet(ReferenceChannel[] channels) : base(channels, sampleRate) { }

        /// <summary>
        /// Fixed sample rate of the Flex HT and HTx.
        /// </summary>
        const int sampleRate = 48000;
    }
}