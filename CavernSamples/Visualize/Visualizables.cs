using Cavern.Filters;
using Cavern.QuickEQ.Utilities;
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
        new("Phase shifted loaded file (forward)", file => PhaseShifter.PhaseShiftInPlace(file, true)),
        new("Envelope", Measurements.GetEnvelope),
    ];
}
