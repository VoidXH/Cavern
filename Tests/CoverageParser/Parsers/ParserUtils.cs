using HtmlAgilityPack;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using CoverageParser.Models;

namespace CoverageParser.Parsers;

/// <summary>
/// Utility class with helper methods for parsing HTML coverage reports.
/// </summary>
public static class ParserUtils {
    /// <summary>
    /// Loads an HTML document from the specified file path with UTF-8 encoding.
    /// </summary>
    /// <param name="filePath">Path to the HTML file to load</param>
    /// <returns>The loaded HtmlDocument.</returns>
    public static HtmlDocument LoadHtml(string filePath) {
        HtmlDocument doc = new HtmlDocument();
        doc.Load(filePath, Encoding.UTF8, true);
        return doc;
    }

    /// <summary>
    /// Parses coverage statistics from a card HTML node containing a coverage stats table.
    /// </summary>
    /// <param name="card">The HTML node representing the card element</param>
    /// <returns>CoverageStats populated with the parsed values.</returns>
    public static CoverageStats ParseCoverageStatsFromCard(HtmlNode card) {
        CoverageStats stats = new CoverageStats();
        HtmlNode table = card.SelectSingleNode(".//table");
        if (table == null) return stats;

        HtmlNodeCollection rows = table.SelectNodes("tr");
        if (rows == null) return stats;

        foreach (HtmlNode row in rows) {
            HtmlNode th = row.SelectSingleNode(".//th");
            HtmlNode td = row.SelectSingleNode(".//td");
            if (th != null && td != null) {
                string key = th.InnerText.Trim().TrimEnd(':');
                string value = td.InnerText.Trim();

                switch (key) {
                    case "Covered lines" or "Covered branches":
                        stats.Covered = ParseInt(value);
                        break;
                    case "Uncovered lines":
                        stats.Uncovered = ParseInt(value);
                        break;
                    case "Coverable lines" or "Total branches":
                        stats.Coverable = ParseInt(value);
                        break;
                    case "Total lines":
                        stats.Total = ParseInt(value);
                        break;
                    case "Line coverage" or "Branch coverage":
                        // Value is like "25.7%" but numbers are in title attribute like "1677 of 6507"
                        string titleAttr = td.GetAttributeValue("title", string.Empty);
                        if (!string.IsNullOrEmpty(titleAttr) && titleAttr.Contains(" of ")) {
                            string[] parts = titleAttr.Split([" of "], StringSplitOptions.None);
                            stats.Covered = ParseInt(parts[0].Trim());
                            stats.Coverable = ParseInt(parts[1].Trim());
                        }
                        Match pctMatch2 = Regex.Match(value, @"(\d+\.?\d*)%");
                        if (pctMatch2.Success) {
                            stats.Percentage = decimal.Parse(pctMatch2.Groups[1].Value, CultureInfo.InvariantCulture);
                        }
                        break;
                }
            }
        }

        // If coverable is 0 or not set correctly, recalculate
        if (stats.Coverable == 0 && stats.Covered + stats.Uncovered > 0) {
            stats.Coverable = stats.Covered + stats.Uncovered;
        }

        return stats;
    }

    /// <summary>
    /// Parses a percentage value from a string, returning 0 for invalid or missing values.
    /// </summary>
    /// <param name="value">The percentage string (e.g., "75.5%")</param>
    /// <returns>The parsed percentage as a decimal, or 0 if the value is invalid.</returns>
    public static decimal ParsePercentage(string value) {
        if (string.IsNullOrEmpty(value) || value == "-" || value == "-%")
            return 0;

        Match match = Regex.Match(value, @"(\d+\.?\d*)%");
        if (match.Success) {
            return decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return 0;
    }

    /// <summary>
    /// Parses an integer value, removing thousand separators (both comma and dot).
    /// </summary>
    /// <param name="value">The integer string that may contain thousand separators</param>
    /// <returns>The parsed integer value.</returns>
    public static int ParseInt(string value) {
        // Remove thousand separators
        string cleaned = value.Replace(",", "").Replace(".", "");
        return int.Parse(cleaned);
    }

    /// <summary>
    /// Finds a class report by assembly name and class name within the coverage report.
    /// </summary>
    /// <param name="report">The coverage report to search</param>
    /// <param name="assembly">The name of the assembly to search in</param>
    /// <param name="className">The name of the class to find</param>
    /// <returns>The matching ClassReport, or null if not found.</returns>
    public static ClassReport FindClassReport(CoverageReport report, string assembly, string className) {
        AssemblyReport assemblyReport = report.Assemblies.FirstOrDefault(a => a.Name == assembly);
        if (assemblyReport == null) return null;

        return assemblyReport.Classes.FirstOrDefault(c => c.FullName == className);
    }
}
