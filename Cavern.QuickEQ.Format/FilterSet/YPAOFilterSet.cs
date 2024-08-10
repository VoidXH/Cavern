using System;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Format.FilterSet.BaseClasses;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
    /// </summary>
    public class YPAOFilterSet : LimitedIIRFilterSet {
        /// <inheritdoc/>
        public override int LFEBands => lfeBands;
        const int lfeBands = 4;

        /// <inheritdoc/>
        public override int Bands => bands;
        const int bands = 7;

        /// <inheritdoc/>
        public override double MaxGain => maxGain;
        const double maxGain = 6;

        /// <inheritdoc/>
        public override double MinGain => minGain;
        const double minGain = -6;

        /// <inheritdoc/>
        public override double GainPrecision => gainPrecision;
        const double gainPrecision = .5;

        /// <inheritdoc/>
        protected override float[] Frequencies => frequencies;

        /// <inheritdoc/>
        protected override float[] QFactors => qFactors;

        /// <summary>
        /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Filter set limited to 1/3 octave band choices for some versions of YPAO.
        /// </summary>
        public YPAOFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// For certain Yamaha RX models between 2015 and 2018, the last 3 filters only start from 500 Hz.
        /// This function calculates the filters in such a way.
        /// </summary>
        /// <param name="target">Curve to approximate with the resulting <see cref="PeakingEQ"/> set</param>
        /// <param name="lfe">The operation is performed for the LFE channel</param>
        /// <param name="sampleRate">Sample rate of the created filters</param>
        /// <remarks>Might return less bands when no better solution can be found in the iteration limit.</remarks>
        public static PeakingEQ[] GetFilters(Equalizer target, bool lfe, int sampleRate) {
            if (lfe) {
                return new PeakingEqualizer(target) {
                    MinGain = minGain,
                    MaxGain = maxGain,
                    GainPrecision = gainPrecision,
                }.GetPeakingEQ(sampleRate, lfeBands);
            }

            const int limitedBands = 3,
                unlimitedBands = bands - limitedBands;
            PeakingEqualizer peqGenerator = new PeakingEqualizer(target) {
                MaxGain = maxGain,
                MinGain = minGain,
                GainPrecision = gainPrecision
            };
            PeakingEQ[] result = peqGenerator.GetPeakingEQ(sampleRate, unlimitedBands);
            ComplexFilter simulator = new ComplexFilter(result.Select(x => x.GetInverse()));
            FilterAnalyzer analyzer = new FilterAnalyzer(simulator, sampleRate);
            Equalizer approximation = analyzer.ToEqualizer(10, 20000, 1 / 24f);
            target.Merge(approximation);

            peqGenerator.MinFrequency = frequencies[lastBandFreqsFrom];
            PeakingEQ[] lastBands = peqGenerator.GetPeakingEQ(sampleRate, limitedBands);
            int firstBands = result.Length;
            Array.Resize(ref result, firstBands + lastBands.Length);
            for (int i = 0; i < lastBands.Length; i++) {
                result[i + firstBands] = lastBands[i];
            }
            return result;
        }

        /// <inheritdoc/>
        public override void Export(string path) => File.WriteAllText(path, Export(false));

        /// <summary>
        /// All the possible bands that can be selected for YPAO. These are 1/3 octaves apart.
        /// </summary>
        static readonly float[] frequencies = {
            15.6f, 19.7f, 24.8f, 31.3f, 39.4f, 49.6f, 62.5f, 78.7f, 99.2f, 125.0f, 157.5f, 198.4f, 250, 315, 396.9f, 500, 630, 793.7f,
            1000, 1260, 1590, 2000, 2520, 3170, 4000, 5040, 6350, 8000, 10100, 12700, 16000
        };

        /// <summary>
        /// The frequency which is the first option in the last few bands.
        /// </summary>
        const int lastBandFreqsFrom = 15;

        /// <summary>
        /// All the possible Q-factors that can be selected for YPAO.
        /// </summary>
        static readonly float[] qFactors = { 0.5f, 0.63f, 0.794f, 1f, 1.26f, 1.587f, 2f, 2.520f, 3.175f, 4f, 5.04f, 6.350f, 8f, 10.08f };
    }
}