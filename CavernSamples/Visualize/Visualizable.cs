using System;

namespace Visualize;

/// <summary>
/// A waveform that can be shown on screen, with a human-readable <paramref name="name"/> that's shown when a dropdown contains this object.
/// </summary>
/// <param name="name">Human-readable name of the visualized waveform</param>
/// <param name="create">Produces the represented waveform</param>
public class Visualizable(string name, Func<float[]> produce) {
    /// <summary>
    /// Produce the represented waveform.
    /// </summary>
    public float[] Produce() => produce();

    /// <inheritdoc/>
    public override string ToString() => name;
}
