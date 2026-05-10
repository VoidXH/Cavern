using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Coverage report for a single method.
/// </summary>
public class MethodReport {
    /// <summary>
    /// Gets or sets the name of the method.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the method is defined.
    /// </summary>
    [JsonPropertyName("line")]
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the branch coverage percentage of the method.
    /// </summary>
    [JsonPropertyName("branchCoverage")]
    public decimal BranchCoverage { get; set; }

    /// <summary>
    /// Gets or sets the C.R.A.P. score of the method.
    /// </summary>
    [JsonPropertyName("crapScore")]
    public decimal CrapScore { get; set; }

    /// <summary>
    /// Gets or sets the cyclomatic complexity of the method.
    /// </summary>
    [JsonPropertyName("cyclomaticComplexity")]
    public int CyclomaticComplexity { get; set; }

    /// <summary>
    /// Gets or sets the line coverage percentage of the method.
    /// </summary>
    [JsonPropertyName("lineCoverage")]
    public decimal LineCoverage { get; set; }
}
