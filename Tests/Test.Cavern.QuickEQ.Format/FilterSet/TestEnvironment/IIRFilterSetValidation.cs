using Cavern.Filters;
using Cavern.Format.FilterSet;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

using Test.Cavern.QuickEQ.Format.FilterSet.Exceptions;

namespace Test.Cavern.QuickEQ.Format.FilterSet.TestEnvironment;

/// <summary>
/// Shared validation helpers for IIR filter set tests.
/// </summary>
internal static class IIRFilterSetValidation {
    /// <summary>
    /// Check that each generated band stays within the device's gain limits and its gain/Q are at the allowed precision.
    /// </summary>
    public static void CheckBandConstraints(IIRFilterSet set, PeakingEQ[] bands) {
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
    public static void CheckExportRoundTrip(PeakingEQ[] source, IIRFilterSet parsed) {
        BiquadFilter[] parsedBands = ((IIRChannelData)parsed.Channels[0]).filters;
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
    /// Check that the parsed filter set's frequency response matches the source <see cref="Equalizer"/>
    /// at each of its bands within the approximation tolerance.
    /// </summary>
    public static void CheckResponseAgainstReference(IIRFilterSet parsed, Equalizer reference, int sampleRate, double toleranceDb) =>
        CheckResponseAgainstReference(parsed, 0, reference, sampleRate, toleranceDb);

    /// <summary>
    /// Check that the parsed filter set's frequency response matches the source <see cref="Equalizer"/>
    /// at each of its bands within the approximation tolerance, for a specific channel.
    /// </summary>
    public static void CheckResponseAgainstReference(IIRFilterSet parsed, int channel, Equalizer reference, int sampleRate, double toleranceDb) {
        BiquadFilter[] parsedBands = ((IIRChannelData)parsed.Channels[channel]).filters;
        if (parsedBands.Length == 0) {
            return;
        }

        const int length = 32768;
        Complex[] processedFFT = parsed.GetConvolutionFilter(sampleRate, length)[channel].FFT();

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
    /// Check if a <paramref name="value"/> is a multiple of the allowed <paramref name="precision"/> in tolerance.
    /// </summary>
    static bool IsPrecise(double value, double precision) {
        const double maxError = .000001;
        double error = value % precision;
        return (-maxError < error && error < maxError) ||
            (precision - maxError < error && error < precision + maxError) ||
            (-precision - maxError < error && error < -precision + maxError);
    }
}
