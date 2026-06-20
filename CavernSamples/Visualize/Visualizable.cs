using System;

namespace Visualize;

/// <summary>
/// A waveform that can be shown on screen, with a human-readable <paramref name="name"/> that's shown when a dropdown contains this object.
/// </summary>
/// <param name="name">Human-readable name of the visualized waveform</param>
/// <param name="produce">Produces the represented waveform, given a reference input (can be null for functions that are generators)</param>
public class Visualizable(string name, Func<float[], float[]> produce) {
    /// <summary>
    /// Produce the represented waveform.
    /// </summary>
    public float[] Produce(float[] reference) => produce(reference);

    /// <inheritdoc/>
    public override string ToString() => name;
}
