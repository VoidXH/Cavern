using Cavern.Utilities;

namespace Cavern.QuickEQ.PolarityCorrections {
    /// <summary>
    /// Generates a polarity correction by checking if pairs of channels add together constructively or destructively.
    /// </summary>
    public sealed class ConstructivityBasedPolarityCorrection : PolarityCorrection {
        /// <inheritdoc/>
        public override bool[] GetInvertedChannels(MultichannelWaveform simulations) {
            bool[] result = new bool[simulations.Channels];
            float[] working = new float[simulations[0].Length];
            for (int i = 1; i < simulations.Channels; i++) {
                simulations[0].CopyTo(working);
                WaveformUtils.Mix(simulations[i], working);
                float constructive = WaveformUtils.GetRMS(working);
                simulations[0].CopyTo(working);
                WaveformUtils.Invert(working);
                WaveformUtils.Mix(simulations[i], working);
                float destructive = WaveformUtils.GetRMS(working);
                if (destructive > constructive) {
                    result[i] = true;
                }
            }
            return result;
        }
    }
}