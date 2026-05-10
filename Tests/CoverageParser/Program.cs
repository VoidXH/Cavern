using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using CoverageParser.Models;
using CoverageParser.Parsers;

namespace CoverageParser;

/// <summary>
/// Entry point for the CoverageParser tool. Parses HTML coverage reports and outputs JSON.
/// </summary>
public static class Program {
    /// <summary>
    /// JSON serialization options with indented output and relaxed escaping.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    /// <summary>
    /// Main entry point. Parses the coverage report directory and writes JSON output.
    /// </summary>
    /// <param name="args">Command-line arguments: [reportDirectory, outputFile]</param>
    public static void Main(string[] args) {
        string reportDirectory = args.Length > 0 ? args[0] : "TestResults/HtmlReport";
        string outputFile = args.Length > 1 ? args[1] : "coverage.json";

        if (!Directory.Exists(reportDirectory)) {
            Console.Error.WriteLine($"Error: Directory '{reportDirectory}' not found.");
            Environment.Exit(1);
        }

        CoverageReport report = new CoverageReport();

        // Parse index.html for summary and assembly-level stats
        IndexHtmlParser.ParseIndexHtml(reportDirectory, report);

        // Parse individual class HTML files for method-level details
        ClassHtmlParser.ParseClassHtmlFiles(reportDirectory, report);

        // Write JSON output
        string json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(outputFile, json, Encoding.UTF8);

        Console.WriteLine($"Coverage report written to {outputFile}");
        Console.WriteLine($"  Assemblies: {report.Summary.AssemblyCount}");
        Console.WriteLine($"  Classes: {report.Summary.ClassCount}");
        Console.WriteLine($"  Risk Hotspots: {report.RiskHotspots.Count}");
    }
}