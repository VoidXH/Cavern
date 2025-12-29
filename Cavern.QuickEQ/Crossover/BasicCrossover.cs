using Cavern.Filters;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A generic crossover used most of the time. This will use Equalizer APO's included lowpass/highpass filters to create the crossover,
    /// but that can be overridden to create any custom mains-to-LFE crossover function.
    /// </summary>
    public class BasicCrossover : Crossover {
        /// <summary>
        /// Create a biquad crossover with frequencies for each channel. Only values over 0 mean crossovered channels.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public BasicCrossover(CrossoverDescription mixing) : base(mixing) { }

        /// <inheritdoc/>
        public override Filter GetHighpassOptimized(int sampleRate, float frequency, int length) => new Highpass(sampleRate, frequency);

        /// <inheritdoc/>
        public override Filter GetLowpassOptimized(int sampleRate, float frequency, int length) => new Lowpass(sampleRate, frequency);
    }
}