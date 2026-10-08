using System;
using System.Collections.Generic;

using Cavern.Filters;
using Cavern.QuickEQ.Crossover.Enums;
using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A generic crossover used most of the time. This will use Equalizer APO's included lowpass/highpass filters to create the crossover,
    /// but that can be overridden to create any custom mains-to-LFE crossover function.
    /// </summary>
    public class BasicCrossover : Crossover {
        /// <summary>
        /// Filter order (must be even). Each order step adds 6 dB/octave. Default is 2 (12 dB/octave).
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Q-factor for each biquad stage. Default is 0.7071 (Butterworth/maximum flatness).
        /// </summary>
        public double Q { get; }

        /// <summary>
        /// Crossover slope type. Determines Q-factor if not explicitly overridden.
        /// </summary>
        public CrossoverSlope Slope { get; }

        /// <summary>
        /// Create a biquad crossover with default 2nd-order Butterworth (12 dB/octave).
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public BasicCrossover(CrossoverDescription mixing) : this(mixing, 2, CrossoverSlope.Butterworth) { }

        /// <summary>
        /// Create a biquad crossover with specified order, defaulting to Butterworth slope.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        public BasicCrossover(CrossoverDescription mixing, int order) : this(mixing, order, CrossoverSlope.Butterworth) { }

        /// <summary>
        /// Create a biquad crossover with specified order and standard slope type.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="slope">Crossover slope type (Butterworth, Linkwitz-Riley, or Bessel)</param>
        public BasicCrossover(CrossoverDescription mixing, int order, CrossoverSlope slope) : this(mixing, order, slope, CrossoverType.Biquad) { }

        /// <summary>
        /// Create a biquad crossover with specified order and custom Q-factor.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="q">Q-factor for each biquad stage</param>
        public BasicCrossover(CrossoverDescription mixing, int order, double q) : this(mixing, order, q, CrossoverType.Biquad) { }

        /// <summary>
        /// Create a crossover with specified order, slope, and type.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="slope">Crossover slope type (Butterworth, Linkwitz-Riley, or Bessel)</param>
        /// <param name="type">The crossover type</param>
        protected BasicCrossover(CrossoverDescription mixing, int order, CrossoverSlope slope, CrossoverType type) : this(mixing, order, slope.GetQFactor(), type) { }

        /// <summary>
        /// Create a crossover with specified order, custom Q-factor, and type.
        /// </summary>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        /// <param name="order">Filter order (must be even, 2 = 12 dB/octave)</param>
        /// <param name="q">Q-factor for each biquad stage</param>
        /// <param name="type">The crossover type</param>
        protected BasicCrossover(CrossoverDescription mixing, int order, double q, CrossoverType type) : base(mixing, type) {
            if (order <= 0 || order % 2 != 0) {
                throw new ArgumentException("Order must be a positive even number", nameof(order));
            }

            Order = order;
            Q = q;
            Slope = CrossoverSlope.Butterworth;
        }

        /// <inheritdoc/>
        public override float[] GetHighpass(int sampleRate, float frequency, int length) => SimulateCascade(sampleRate, frequency, true, length);

        /// <inheritdoc/>
        public override float[] GetLowpass(int sampleRate, float frequency, int length) => SimulateCascade(sampleRate, frequency, false, length);

        /// <inheritdoc/>
        public override Filter GetHighpassOptimized(int sampleRate, float frequency, int length) => CreateCascade(sampleRate, frequency, true);

        /// <inheritdoc/>
        public override Filter GetLowpassOptimized(int sampleRate, float frequency, int length) => CreateCascade(sampleRate, frequency, false);

        /// <inheritdoc/>
        public override void AddHighpass(List<string> wipConfig, float frequency) {
            string hpf = $"Filter: Filter: ON HPQ Fc {frequency} Hz Q {Q}";
            for (int i = 0; i < Order / 2; i++) {
                wipConfig.Add(hpf);
            }
        }

        /// <inheritdoc/>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            string lpf = $"Filter: Filter: ON LPQ Fc {frequency} Hz Q {Q}";
            for (int i = 0; i < Order / 2; i++) {
                wipConfig.Add(lpf);
            }
            AddExtraOperations(wipConfig);
        }

        /// <summary>
        /// Create a cascade of biquad filters for the specified order.
        /// </summary>
        protected Filter CreateCascade(int sampleRate, float frequency, bool highpass) {
            int stages = Order / 2;
            if (stages == 1) {
                if (highpass) {
                    return new Highpass(sampleRate, frequency, Q);
                }
                return new Lowpass(sampleRate, frequency, Q);
            }

            ComplexFilter cascade = new ComplexFilter();
            for (int i = 0; i < stages; i++) {
                if (highpass) {
                    cascade.Filters.Add(new Highpass(sampleRate, frequency, Q));
                } else {
                    cascade.Filters.Add(new Lowpass(sampleRate, frequency, Q));
                }
            }
            return cascade;
        }

        /// <summary>
        /// Simulate the cascade of biquad filters to generate an impulse response.
        /// </summary>
        float[] SimulateCascade(int sampleRate, float frequency, bool highpass, int length) {
            float[] impulse = WaveformGenerator.DiracDelta(length);
            Filter cascade = CreateCascade(sampleRate, frequency, highpass);
            cascade.Process(impulse);
            return impulse;
        }
    }
}
