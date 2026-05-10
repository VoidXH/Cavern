using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Represents a risk hotspot identified by high C.R.A.P. score and complexity.
/// </summary>
public class RiskHotspot {
    /// <summary>
    /// Gets or sets the name of the assembly containing the hotspot.
    /// </summary>
    [JsonPropertyName("assembly")]
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the class containing the hotspot.
    /// </summary>
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the method containing the hotspot.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the C.R.A.P. (Change, Risk, Acceptable Points) score.
    /// </summary>
    [JsonPropertyName("crapScore")]
    public decimal CrapScore { get; set; }

    /// <summary>
    /// Gets or sets the cyclomatic complexity of the method.
    /// </summary>
    [JsonPropertyName("cyclomaticComplexity")]
    public int CyclomaticComplexity { get; set; }
}
