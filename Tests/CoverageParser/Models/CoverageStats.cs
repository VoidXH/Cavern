using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Coverage statistics for a specific metric (line, branch, etc.).
/// </summary>
public class CoverageStats {
    /// <summary>
    /// Gets or sets the number of covered items.
    /// </summary>
    [JsonPropertyName("covered")]
    public int Covered { get; set; }

    /// <summary>
    /// Gets or sets the number of uncovered items.
    /// </summary>
    [JsonPropertyName("uncovered")]
    public int Uncovered { get; set; }

    /// <summary>
    /// Gets or sets the total number of coverable items.
    /// </summary>
    [JsonPropertyName("coverable")]
    public int Coverable { get; set; }

    /// <summary>
    /// Gets or sets the total number of items (covered + uncovered + any other total).
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the coverage percentage.
    /// </summary>
    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }
}
