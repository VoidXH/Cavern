using System.Collections.Generic;

using Cavern.Filters;
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
        /// Add the filter's interpretation of highpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        public override void AddHighpass(List<string> wipConfig, float frequency) {
            ComplexFilter bw = new ComplexFilter();
            bw.Filters.Add(new Highpass(48000, frequency));
            bw.Filters.Add(new Highpass(48000, frequency));
            wipConfig.Add(new FilterAnalyzer(bw, 48000).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());
        }

        /// <summary>
        /// Add the filter's interpretation of lowpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        /// <remarks>Don't forget to call AddExtraOperations, this is generally the best place for it.</remarks>
        public override void AddLowpass(List<string> wipConfig, float frequency) {
            ComplexFilter bw = new ComplexFilter();
            bw.Filters.Add(new Lowpass(48000, frequency));
            bw.Filters.Add(new Lowpass(48000, frequency));
            wipConfig.Add(new FilterAnalyzer(bw, 48000).ToEqualizer(10, 480, 1 / 24.0).ExportToEqualizerAPO());
            AddExtraOperations(wipConfig);
        }
    }
}