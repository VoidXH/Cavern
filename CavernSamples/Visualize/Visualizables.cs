using Cavern.Filters;

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
        new("Phase shifter (Hilbert transform, forward)", () => PhaseShifter.GenerateFilter(Samples, true)),
        new("Phase shifter (Hilbert transform, reverse)", () => PhaseShifter.GenerateFilter(Samples, false)),
    ];
}
