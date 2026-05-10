using System.Text.Json.Serialization;

using CoverageParser.Models;

namespace CoverageParser;

/// <summary>
/// Coverage report for a single assembly (DLL).
/// </summary>
public class AssemblyReport {
    /// <summary>
    /// Gets or sets the name of the assembly.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line coverage statistics for the assembly.
    /// </summary>
    [JsonPropertyName("lineCoverage")]
    public CoverageStats LineCoverage { get; set; } = new();

    /// <summary>
    /// Gets or sets the branch coverage statistics for the assembly.
    /// </summary>
    [JsonPropertyName("branchCoverage")]
    public CoverageStats BranchCoverage { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of class-level coverage reports within the assembly.
    /// </summary>
    [JsonPropertyName("classes")]
    public List<ClassReport> Classes { get; set; } = [];
}
