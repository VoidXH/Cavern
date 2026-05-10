using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Overall summary of the coverage report.
/// </summary>
public class ReportSummary {
    /// <summary>
    /// Gets or sets the name of the parser tool used to generate the report.
    /// </summary>
    [JsonPropertyName("parser")]
    public string Parser { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of assemblies.
    /// </summary>
    [JsonPropertyName("assemblyCount")]
    public int AssemblyCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of classes.
    /// </summary>
    [JsonPropertyName("classCount")]
    public int ClassCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of files.
    /// </summary>
    [JsonPropertyName("fileCount")]
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the date when the coverage report was generated.
    /// </summary>
    [JsonPropertyName("coverageDate")]
    public string CoverageDate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line coverage statistics.
    /// </summary>
    [JsonPropertyName("lineCoverage")]
    public CoverageStats LineCoverage { get; set; } = new();

    /// <summary>
    /// Gets or sets the branch coverage statistics.
    /// </summary>
    [JsonPropertyName("branchCoverage")]
    public CoverageStats BranchCoverage { get; set; } = new();
}
