using System.Windows.Media;

namespace Cavern.WPF.Controls.Models;

/// <summary>
/// A legend entry: the name and color of a displayed curve.
/// </summary>
public class LegendEntry {
    /// <summary>
    /// Name of the curve.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Color of the curve.
    /// </summary>
    public Brush Swatch { get; }

    /// <summary>
    /// Create a legend entry.
    /// </summary>
    /// <param name="name">Name of the curve.</param>
    /// <param name="swatch">Color of the curve.</param>
    internal LegendEntry(string name, Brush swatch) {
        Name = name;
        Swatch = swatch;
    }
}
