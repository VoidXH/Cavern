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
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public BasicCrossover(float[] frequencies, bool[] subs) : base(frequencies, subs) { }

        /// <inheritdoc/>
        public override Filter GetHighpassOptimized(int sampleRate, float frequency, int length) => new Highpass(sampleRate, frequency);

        /// <inheritdoc/>
        public override Filter GetLowpassOptimized(int sampleRate, float frequency, int length) => new Lowpass(sampleRate, frequency);
    }
}