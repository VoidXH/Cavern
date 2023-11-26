using Cavern.Utilities;

namespace Cavern.QuickEQ.PolarityCorrections {
    /// <summary>
    /// Generates a polarity correction by aligning impulse peaks to the same direction.
    /// </summary>
    /// <remarks>This is a naive approach and should only be used when performance is more important than precision.</remarks>
    public sealed class ImpulsePeakBasedPolarityCorrection : PolarityCorrection {
        /// <inheritdoc/>
        public override bool[] GetInvertedChannels(MultichannelWaveform simulations) {
            bool[] result = new bool[simulations.Channels];
            for (int i = 0; i < result.Length; i++) {
                result[i] = WaveformUtils.GetPeakSigned(simulations[i]) < 0;
            }
            return result;
        }
    }
}