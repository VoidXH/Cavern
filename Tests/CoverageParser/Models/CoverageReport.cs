using System.Text.Json.Serialization;

namespace CoverageParser.Models;

/// <summary>
/// Top-level coverage report data.
/// </summary>
public class CoverageReport {
    /// <summary>
    /// Gets or sets the overall summary of the coverage report.
    /// </summary>
    [JsonPropertyName("summary")]
    public ReportSummary Summary { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of assembly-level coverage reports.
    /// </summary>
    [JsonPropertyName("assembly")]
    public List<AssemblyReport> Assemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of risk hotspots identified in the codebase.
    /// </summary>
    [JsonPropertyName("riskHotspots")]
    public List<RiskHotspot> RiskHotspots { get; set; } = [];
}
