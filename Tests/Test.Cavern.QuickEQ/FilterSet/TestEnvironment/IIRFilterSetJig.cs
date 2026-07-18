using Cavern;
using Cavern.Filters;
using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

using Test.Cavern.Consts;
using Test.Cavern.QuickEQ.FilterSet.Exceptions;

using FSet = Cavern.Format.FilterSet.FilterSet;

namespace Test.Cavern.QuickEQ.FilterSet.TestEnvironment;

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

        CheckBandConstraints(set, bands);
        string result = set.Export();
        IIRFilterSet parsed = new(result, Listener.DefaultSampleRate);
        CheckExportRoundTrip(bands, parsed);
        CheckResponseAgainstReference(parsed, reference, set.SampleRate, Tolerance);
    });

    /// <summary>
    /// Check that each generated band stays within the device's gain limits and its gain/Q are at the allowed precision.
    /// </summary>
    static void CheckBandConstraints(IIRFilterSet set, PeakingEQ[] bands) {
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
    }

    /// <summary>
    /// Check that every band survives the export's rounding precision.
    /// </summary>
    static void CheckExportRoundTrip(PeakingEQ[] source, IIRFilterSet parsed) {
        BiquadFilter[] parsedBands = ((IIRFilterSet.IIRChannelData)parsed.Channels[0]).filters;
        Assert.AreEqual(source.Length, parsedBands.Length, "The exported and parsed band counts must match.");

        const double epsilon = 0.0005;
        for (int i = 0; i < source.Length; i++) {
            double freqTolerance = (source[i].CenterFreq < 100 ? 0.005 : (source[i].CenterFreq < 1000 ? 0.05 : 0.5)) + epsilon;
            Assert.AreEqual(source[i].CenterFreq, parsedBands[i].CenterFreq, freqTolerance, $"Band {i + 1} frequency drifted beyond the export's rounding precision.");
            Assert.AreEqual(source[i].Gain, parsedBands[i].Gain, 0.005 + epsilon, $"Band {i + 1} gain drifted beyond the export's rounding precision.");
            Assert.AreEqual(source[i].Q, parsedBands[i].Q, 0.005 + epsilon, $"Band {i + 1} Q factor drifted beyond the export's rounding precision.");
        }
    }

    /// <summary>
    /// Check that the parsed filter set's frequency response matches the source <see cref="reference"/>
    /// <see cref="Equalizer"/> at each of its bands within the approximation tolerance.
    /// </summary>
    static void CheckResponseAgainstReference(IIRFilterSet parsed, Equalizer reference, int sampleRate, double toleranceDb) {
        BiquadFilter[] parsedBands = ((IIRFilterSet.IIRChannelData)parsed.Channels[0]).filters;
        if (parsedBands.Length == 0) {
            return;
        }

        const int length = 32768;
        Complex[] processedFFT = parsed.GetConvolutionFilter(sampleRate, length)[0].FFT();

        IReadOnlyList<Band> bands = reference.Bands;
        for (int i = 1; i < bands.Count - 1; i++) {
            Band band = bands[i];
            int bin = (int)(band.Frequency * length / sampleRate);
            if (bin <= 0 || bin >= length / 2) {
                continue;
            }
            double processedGain = QMath.GainToDb(processedFFT[bin].Magnitude);
            Assert.AreEqual(reference[band.Frequency], processedGain, toleranceDb, $"Band at {band.Frequency} Hz differs from the source Equalizer beyond tolerance.");
        }
    }

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
