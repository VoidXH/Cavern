using Cavern.Filters;
using Cavern.QuickEQ;
using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;

namespace Visualize;

/// <summary>
/// <see cref="Database"/> of all functions that this program can visualize.
/// </summary>
public static class Visualizables {
    /// <summary>
    /// How many samples to generate when a waveform is made.
    /// </summary>
    public static int Samples { get; set; } = 1024;

    /// <summary>
    /// All supported functions for visualization.
    /// </summary>
    public static readonly Visualizable[] Database = [
        new("Phase shifter (Hilbert transform, forward)", _ => PhaseShifter.GenerateFilter(Samples, true)),
        new("Phase shifter (Hilbert transform, reverse)", _ => PhaseShifter.GenerateFilter(Samples, false)),
    ];

    /// <summary>
    /// Display the loaded audio file.
    /// </summary>
    public static readonly Visualizable rawFile = new("Loaded audio file", x => x);

    /// <summary>
    /// All supported functions for processing of loaded audio files.
    /// </summary>
    public static readonly Visualizable[] DatabaseForFiles = [
        rawFile,
        new("Envelope (raw)", Measurements.GetEnvelope),
        new("Envelope (windowed)", file => GetWindowed(Measurements.GetEnvelope(file), Window.Tukey, 64)),
        new("Phase (unwrapped)", file => GetUnwrapped(Measurements.GetPhase(file.FFT()))),
        new("Phase (wrapped)", file => Measurements.GetPhase(file.FFT())),
        new("Phase-shifted loaded file (forward)", file => PhaseShifter.PhaseShiftInPlace(file, true)),
    ];

    /// <summary>
    /// Unwrap a <paramref name="phase"/> curve.
    /// </summary>
    static float[] GetUnwrapped(float[] phase) {
        Measurements.UnwrapPhase(phase);
        return phase;
    }

    /// <summary>
    /// Window a <paramref name="curve"/> around its peak with a specific window <paramref name="function"/> and given <paramref name="length"/> to each direction.
    /// </summary>
    static float[] GetWindowed(float[] curve, Window function, int length) {
        float[] result = curve.FastClone();
        int peak = DelayCalculation.GetImpulsePeakDelay(curve);
        Windowing.ApplyWindow(result, function, function, peak - length, peak, peak + length);
        return result;
    }
}
