using Cavern;
using Cavern.Filters;
using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Equalization;

using Test.Cavern.QuickEQ.FilterSet.Exceptions;

using FSet = Cavern.Format.FilterSet.FilterSet;

namespace Test.Cavern.QuickEQ.FilterSet.TestEnvironment {
    /// <summary>
    /// Test framework for IIR filter sets.
    /// </summary>
    /// <param name="target">Device to test</param>
    public abstract class IIRFilterSetJig(FilterSetTarget target) {
        /// <summary>
        /// Check if a <paramref name="value"/> is a multiple of the allowed <paramref name="precision"/> in tolerance.
        /// </summary>
        /// <returns></returns>
        static bool IsPrecise(double value, double precision) {
            const double maxError = 0.000001;
            double error = value % precision;
            return (-maxError < error && error < maxError) ||
                (precision - maxError < error && error < precision + maxError) ||
                (-precision - maxError < error && error < -precision + maxError);
        }

        /// <summary>
        /// Use the <see cref="PeakingEqualizer"/> to approximate a known <see cref="reference"/> filter.
        /// This test is large, because data that is heavy to compute is reused for further calculations.
        /// <list type="bullet">
        /// <item>Test if frequencies are properly rounded and in limits.</item>
        /// <item>Test if Q factors are properly rounded.</item>
        /// </list>
        /// </summary>
        [TestMethod, Timeout(10000)]
        public void TestEQBase() {
            FSet sourceSet = FSet.Create(target, 1, Listener.DefaultSampleRate);
            if (sourceSet is not IIRFilterSet set) {
                throw new InvalidCastException();
            }

            PeakingEqualizer eq = new(reference) {
                MinGain = set.MinGain,
                MaxGain = set.MaxGain,
                GainPrecision = set.GainPrecision,
                StartQ = set.CenterQ,
                PostprocessFilter = set.PostprocessFilter,
            };
            PeakingEQ[] bands = eq.GetPeakingEQ(set.SampleRate, set.Bands);
            set.SetupChannel(0, bands);

            for (int i = 0; i < bands.Length; i++) {
                double gain = bands[i].Gain;
                if (gain < set.MinGain || gain > set.MaxGain) {
                    throw new GainOutOfRangeException(gain, set.MinGain, set.MaxGain);
                }
                if (!IsPrecise(gain, set.GainPrecision)) {
                    throw new GainImpreciseException(gain, set.GainPrecision);
                }
                if (!IsPrecise(bands[i].Q, set.QPrecision)) {
                    throw new QImpreciseException(gain, set.QPrecision);
                }
            }

            string result = set.Export();
            // TODO: parse and check tolerance (set in private property)
        }

        /// <summary>
        /// Reference filter to approximate.
        /// </summary>
        static readonly Equalizer reference = new Equalizer([
            new Band(20, 30),
            new Band(100, -10),
            new Band(200, 2),
            new Band(500, -4),
            new Band(600, 4),
            new Band(700, -4),
            new Band(1000, 2),
            new Band(10000, 0)
        ], true);
    }
}
