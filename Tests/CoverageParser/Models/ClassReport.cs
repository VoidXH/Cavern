using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Coverage report for a single class.
/// </summary>
public class ClassReport {
    /// <summary>
    /// Gets or sets the fully qualified name of the class.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the assembly containing the class.
    /// </summary>
    [JsonPropertyName("assembly")]
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path of the class source file.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the line coverage statistics for the class.
    /// </summary>
    [JsonPropertyName("lineCoverage")]
    public CoverageStats LineCoverage { get; set; } = new();

    /// <summary>
    /// Gets or sets the branch coverage statistics for the class.
    /// </summary>
    [JsonPropertyName("branchCoverage")]
    public CoverageStats BranchCoverage { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of method-level coverage reports within the class.
    /// </summary>
    [JsonPropertyName("methods")]
    public List<MethodReport> Methods { get; set; } = [];
}
