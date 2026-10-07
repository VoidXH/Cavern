using System.Collections.Generic;

using Cavern.Filters;
using Cavern.QuickEQ.Crossover.Enums;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// The generally used 2nd order highpass/lowpass, but without the phase distortions by applying the spectrum with FIR filters.
    /// </summary>
    public class SyntheticBiquadCrossover : BasicCrossover {
        /// <summary>
        /// Creates a phase distortion-less <see cref="BasicCrossover"/> with default 2nd-order Butterworth (12 dB/octave).
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public SyntheticBiquadCrossover(CrossoverDescription mixing) : base(mixing, 2, CrossoverSlope.Butterworth) { }

        /// <summary>
        /// Creates a phase distortion-less <see cref="BasicCrossover"/> with specified order, defaulting to Butterworth slope.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        public SyntheticBiquadCrossover(CrossoverDescription mixing, int order) : base(mixing, order, CrossoverSlope.Butterworth) { }

        /// <summary>
        /// Creates a phase distortion-less <see cref="BasicCrossover"/> with specified order and custom Q-factor.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="q">Q-factor for each biquad stage</param>
        public SyntheticBiquadCrossover(CrossoverDescription mixing, int order, double q) : base(mixing, order, q) { }

        /// <summary>
        /// Creates a phase distortion-less <see cref="BasicCrossover"/> with specified order and standard slope type.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="slope">Crossover slope type (Butterworth, Linkwitz-Riley, or Bessel)</param>
        public SyntheticBiquadCrossover(CrossoverDescription mixing, int order, CrossoverSlope slope) : base(mixing, order, slope) { }

        /// <summary>
        /// Get a <see cref="FilterAnalyzer"/> instance for a cascade of biquad filters.
        /// </summary>
        static FilterAnalyzer GetAnalyzer(int sampleRate, float frequency, bool highpass, int stages, double q) {
            ComplexFilter filters = new ComplexFilter();
            for (int i = 0; i < stages; i++) {
                BiquadFilter filter = highpass
                    ? (BiquadFilter)new Highpass(sampleRate, frequency, q)
                    : new Lowpass(sampleRate, frequency, q);
                filters.Filters.Add(filter);
            }
            return new FilterAnalyzer(filters, sampleRate);
        }

        /// <summary>
        /// Get a FIR filter for a cascade of biquad filters' response in minimum phase.
        /// </summary>
        static float[] GetImpulse(int sampleRate, float frequency, bool highpass, int stages, double q, int length) {
            FilterAnalyzer analyzer = GetAnalyzer(sampleRate, frequency, highpass, stages, q);
            analyzer.Resolution = length;
            return analyzer.ToEqualizer(10, 20000, 1 / 12.0).GetConvolution(sampleRate, length);
        }

        /// <inheritdoc/>
        public override void AddHighpass(List<string> wipConfig, float frequency) {
            int stages = Order / 2;
            wipConfig.Add(GetAnalyzer(48000, frequency, true, stages, Q).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());
        }

        /// <inheritdoc/>
        public override float[] GetHighpass(int sampleRate, float frequency, int length) {
            int stages = Order / 2;
            return GetImpulse(sampleRate, frequency, true, stages, Q, length);
        }

        /// <inheritdoc/>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            int stages = Order / 2;
            wipConfig.Add(GetAnalyzer(48000, frequency, false, stages, Q).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());
            AddExtraOperations(wipConfig);
        }

        /// <inheritdoc/>
        public override float[] GetLowpass(int sampleRate, float frequency, int length) {
            int stages = Order / 2;
            return GetImpulse(sampleRate, frequency, false, stages, Q, length);
        }

        /// <inheritdoc/>
        public override Filter GetHighpassOptimized(int sampleRate, float frequency, int length) =>
            new FastConvolver(GetHighpass(sampleRate, frequency, length), sampleRate, 0);

        /// <inheritdoc/>
        public override Filter GetLowpassOptimized(int sampleRate, float frequency, int length) =>
            new FastConvolver(GetLowpass(sampleRate, frequency, length), sampleRate, 0);
    }
}
