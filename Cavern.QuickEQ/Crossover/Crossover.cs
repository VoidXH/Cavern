using System;

using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.QuickEQ.Crossover.Enums;
using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ.Crossover {
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
        /// Shows how the crossover is realized.
        /// </summary>
        public CrossoverType Type { get; } = CrossoverType.Disabled;

        /// <summary>
        /// Create a crossover with frequencies for each channel.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="type">The type of crossover to use</param>
        protected Crossover(CrossoverDescription mixing, CrossoverType type) {
            Mixing = mixing;
            Type = type;
        }

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
                CrossoverType.Disabled => new DisabledCrossover(mixing),
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Create the appropriate type of <see cref="Crossover"/> object for the selected <paramref name="type"/> with custom order and slope.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave). Only used for Biquad and SyntheticBiquad types.</param>
        /// <param name="slope">Crossover slope type. Only used for Biquad and SyntheticBiquad types.</param>
        public static Crossover Create(CrossoverType type, CrossoverDescription mixing, int order, CrossoverSlope slope) {
            return type switch {
                CrossoverType.Biquad => new BasicCrossover(mixing, order, slope),
                CrossoverType.Cavern => new CavernCrossover(mixing),
                CrossoverType.SyntheticBiquad => new SyntheticBiquadCrossover(mixing, order, slope),
                CrossoverType.Disabled => new DisabledCrossover(mixing),
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Generate a 2nd order impulse response for a simple filter.
        /// </summary>
        static float[] Simulate(BiquadFilter filter, int length) {
            float[] impulse = WaveformGenerator.DiracDelta(length);
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
        public abstract float[] GetHighpass(int sampleRate, float frequency, int length);

        /// <summary>
        /// Get the most quickly processed version of this crossover's highpass.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples, if the filter can only be synthesized as a convolution</param>
        public abstract Filter GetHighpassOptimized(int sampleRate, float frequency, int length);

        /// <summary>
        /// Get a FIR filter for the lowpass part of the crossover.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        public abstract float[] GetLowpass(int sampleRate, float frequency, int length);

        /// <summary>
        /// Get the most quickly processed version of this crossover's lowpass.
        /// </summary>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples, if the filter can only be synthesized as a convolution</param>
        public abstract Filter GetLowpassOptimized(int sampleRate, float frequency, int length);

        /// <summary>
        /// Use this value to mix crossover results to an LFE channel.
        /// The LFE's level is over the mains with 10 dB, this results in level matching.
        /// </summary>
        protected internal const float minus10dB = .31622776601f;

        /// <summary>
        /// Generate an impulse response for the lowpass part of a crossover.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        public static float[] GetLowpass(CrossoverType type, int sampleRate, float frequency, int length) =>
            Create(type, null).GetLowpass(sampleRate, frequency, length);

        /// <summary>
        /// Generate an impulse response for the lowpass part of a crossover with custom order and slope.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Lowpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave). Only used for Biquad and SyntheticBiquad types.</param>
        /// <param name="slope">Crossover slope type. Only used for Biquad and SyntheticBiquad types.</param>
        public static float[] GetLowpass(CrossoverType type, int sampleRate, float frequency, int length, int order, CrossoverSlope slope) =>
            Create(type, null, order, slope).GetLowpass(sampleRate, frequency, length);

        /// <summary>
        /// Generate an impulse response for the highpass part of a crossover.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Highpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        public static float[] GetHighpass(CrossoverType type, int sampleRate, float frequency, int length) =>
            Create(type, null).GetHighpass(sampleRate, frequency, length);

        /// <summary>
        /// Generate an impulse response for the highpass part of a crossover with custom order and slope.
        /// </summary>
        /// <param name="type">The type of crossover to use</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="frequency">Highpass cutoff point</param>
        /// <param name="length">Filter length in samples</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave). Only used for Biquad and SyntheticBiquad types.</param>
        /// <param name="slope">Crossover slope type. Only used for Biquad and SyntheticBiquad types.</param>
        public static float[] GetHighpass(CrossoverType type, int sampleRate, float frequency, int length, int order, CrossoverSlope slope) =>
            Create(type, null, order, slope).GetHighpass(sampleRate, frequency, length);
    }
}
