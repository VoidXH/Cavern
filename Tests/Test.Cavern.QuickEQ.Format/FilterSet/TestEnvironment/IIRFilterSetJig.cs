using Cavern;
using Cavern.Filters;
using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Equalization;

using Test.Cavern.Consts;

using FSet = Cavern.Format.FilterSet.FilterSet;

namespace Test.Cavern.QuickEQ.Format.FilterSet.TestEnvironment;

/// <summary>
/// Test framework for IIR filter sets. Validates that a known source Equalizer is correctly approximated,
/// exported, parsed back, and that the parsed filter set reproduces the source response within tolerance.
/// </summary>
/// <param name="target">Device to test</param>
public abstract class IIRFilterSetJig(FilterSetTarget target) {
    /// <summary>
    /// Maximum allowed deviation in dB when comparing the parsed filter set's frequency response against the source <see cref="reference"/> Equalizer.
    /// </summary>
    protected double Tolerance { get; set; } = 1;

    /// <summary>
    /// Use the <see cref="PeakingEqualizer"/> to approximate a known <see cref="reference"/> filter.
    /// This test is large, because data that is heavy to compute is reused for further calculations.
    /// <list type="bullet">
    /// <item>Test if frequencies are properly rounded and in limits.</item>
    /// <item>Test if Q factors are properly rounded.</item>
    /// <item>Test if the exported and parsed filter set reproduces the source response.</item>
    /// </list>
    /// </summary>
    [TestMethod, Timeout(20000)]
    public void TestEQBase() => CavernAmpTest.Run(() => {
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

        IIRFilterSetValidation.CheckBandConstraints(set, bands);
        string result = set.Export();
        bool isGainOnly = result.Contains(" Hz:\t");
        double defaultQ = bands.Length > 0 ? bands[0].Q : set.CenterQ;
        IIRFilterSet parsed = new(result, Listener.DefaultSampleRate, defaultQ);
        IIRFilterSetValidation.CheckExportRoundTrip(bands, parsed, isGainOnly);
        if (!isGainOnly) {
            IIRFilterSetValidation.CheckResponseAgainstReference(parsed, reference, set.SampleRate, Tolerance);
        }
    });

    /// <summary>
    /// Reference filter to approximate.
    /// </summary>
    static readonly Equalizer reference = new Equalizer([
        new Band(20, 2),
        new Band(100, -10),
        new Band(200, 2),
        new Band(500, -4),
        new Band(600, 4),
        new Band(700, -4),
        new Band(1000, 2),
        new Band(10000, 0)
    ], true);
}
