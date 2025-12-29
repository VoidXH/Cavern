using System;

using Cavern.Filters;
using Cavern.Filters.Interfaces;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Supported types of crossovers.
    /// </summary>
    public enum CrossoverType {
        /// <summary>
        /// No crossover is applied.
        /// </summary>
        Disabled,
        /// <summary>
        /// Crossover made of generic 2nd order highpass/lowpass filters.
        /// </summary>
        Biquad,
        /// <summary>
        /// Brickwall FIR crossover.
        /// </summary>
        Cavern,
        /// <summary>
        /// FIR realization of <see cref="Biquad"/>, without any phase distortions.
        /// </summary>
        SyntheticBiquad
    }

    /// <summary>
    /// A crossover to be exported as FIR filters or written into an Equalizer APO configuration file.
    /// </summary>
    public abstract partial class Crossover : IEqualizerAPOFilter {
        /// <summary>
        /// Which channel indices are crossovered at each frequency where a crossover exists.
        /// </summary>
        public (float frequency, int[] channels)[] CrossoverGroups => crossoverGroups ??= Mixing.ConvertToGroups();
        (float, int[])[] crossoverGroups;

        /// <summary>
        /// Which channels to mix to, and which channels to mix from at what crossover frequency.
        /// </summary>
        public CrossoverDescription Mixing { get; }

        /// <summary>
        /// Create a crossover with frequencies for each channel.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        protected Crossover(CrossoverDescription mixing) => Mixing = mixing;

        /// <summary>
        /// Create the appropriate type of <see cref="Crossover"/> object for the selected <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public static Crossover Create(CrossoverType type, CrossoverDescription mixing) {
            return type switch {
                CrossoverType.Biquad => new BasicCrossover(mixing),
                CrossoverType.Cavern => new CavernCrossover(mixing),
                CrossoverType.SyntheticBiquad => new SyntheticBiquadCrossover(mixing),
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Generate a 2nd order impulse response for a simple filter.
        /// </summary>
        static float[] Simulate(BiquadFilter filter, int length) {
            float[] impulse = new float[length];
            impulse[0] = 1;
            filter.Process(impulse);
            ((BiquadFilter)filter.Clone()).Process(impulse);
            return impulse;
        }

        /// <summary>
        /// Get a FIR filter for the highpass part of the crossover.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Highpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        public virtual float[] GetHighpass(int sampleRate, float frequency, int length) =>
            Simulate(new Highpass(sampleRate, frequency), length);

        /// <summary>
        /// Get the most quickly processed version of this crossover's highpass.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples, if the filter can only be synthesized as a convolution</param>
        public virtual Filter GetHighpassOptimized(int sampleRate, float frequency, int length) =>
            new FastConvolver(GetHighpass(sampleRate, frequency, length), sampleRate, 0);

        /// <summary>
        /// Get a FIR filter for the lowpass part of the crossover.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        public virtual float[] GetLowpass(int sampleRate, float frequency, int length) =>
            Simulate(new Lowpass(sampleRate, frequency), length);

        /// <summary>
        /// Get the most quickly processed version of this crossover's lowpass.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples, if the filter can only be synthesized as a convolution</param>
        public virtual Filter GetLowpassOptimized(int sampleRate, float frequency, int length) =>
            new FastConvolver(GetLowpass(sampleRate, frequency, length), sampleRate, 0);

        /// <summary>
        /// Use this value to mix crossover results to an LFE channel.
        /// The LFE's level is over the mains with 10 dB, this results in level matching.
        /// </summary>
        protected internal const float minus10dB = .31622776601f;
    }
}