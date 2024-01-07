using System.Collections.Generic;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// The generally used 2nd order highpass/lowpass, but without the phase distortions by applying the spectrum with FIR filters.
    /// </summary>
    public class SyntheticBiquadCrossover : BasicCrossover {
        /// <summary>
        /// Creates a phase distortion-less <see cref="BasicCrossover"/>.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public SyntheticBiquadCrossover(float[] frequencies, bool[] subs) : base(frequencies, subs) { }

        /// <summary>
        /// Get a <see cref="FilterAnalyzer"/> instance for a 2nd order <see cref="BiquadFilter"/>.
        /// </summary>
        static FilterAnalyzer GetAnalyzer(BiquadFilter filter) {
            ComplexFilter bw = new ComplexFilter();
            bw.Filters.Add(filter);
            bw.Filters.Add((BiquadFilter)filter.Clone());
            return new FilterAnalyzer(bw, filter.SampleRate);
        }

        /// <summary>
        /// Get a FIR filter for a 2nd order biquad filter's response in minimum phase.
        /// </summary>
        static float[] GetImpulse(BiquadFilter filter, int length) {
            FilterAnalyzer analyzer = GetAnalyzer(filter);
            analyzer.Resolution = length;
            return analyzer.ToEqualizer(10, 20000, 1 / 12.0).GetConvolution(filter.SampleRate, length);
        }

        /// <inheritdoc/>
        public override void AddHighpass(List<string> wipConfig, float frequency) =>
            wipConfig.Add(GetAnalyzer(new Highpass(48000, frequency)).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());

        /// <inheritdoc/>
        public override float[] GetHighpass(int sampleRate, float frequency, int length) =>
            GetImpulse(new Highpass(sampleRate, frequency), length);

        /// <inheritdoc/>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            wipConfig.Add(GetAnalyzer(new Lowpass(48000, frequency)).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());
            AddExtraOperations(wipConfig);
        }

        /// <inheritdoc/>
        public override float[] GetLowpass(int sampleRate, float frequency, int length) =>
            GetImpulse(new Lowpass(sampleRate, frequency), length);
    }
}