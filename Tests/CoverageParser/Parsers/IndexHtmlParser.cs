using HtmlAgilityPack;
using System.Globalization;

using CoverageParser.Models;

namespace CoverageParser.Parsers;

/// <summary>
/// Parses index.html from a coverage report directory to extract summary, risk hotspots, and assembly/class data.
/// </summary>
public static class IndexHtmlParser {
    /// <summary>
    /// Parses the index.html file from the report directory and populates the coverage report.
    /// </summary>
    /// <param name="reportDirectory">Path to the directory containing index.html</param>
    /// <param name="report">CoverageReport to populate with parsed data</param>
    public static void ParseIndexHtml(string reportDirectory, CoverageReport report) {
        string indexFile = Path.Combine(reportDirectory, "index.html");
        if (!File.Exists(indexFile)) {
            Console.Error.WriteLine("Warning: index.html not found.");
            return;
        }

        HtmlDocument doc = ParserUtils.LoadHtml(indexFile);

        // Parse summary information
        ParseSummary(doc, report);

        // Parse risk hotspots
        ParseRiskHotspots(doc, report);

        // Parse coverage table for assembly and class data
        ParseCoverageTable(doc, report);
    }

    /// <summary>
    /// Parses the summary section from the index.html document.
    /// </summary>
    /// <param name="doc">The parsed HTML document</param>
    /// <param name="report">CoverageReport to populate with summary data</param>
    private static void ParseSummary(HtmlDocument doc, CoverageReport report) {
        // Parser info table - values are in title attribute
        HtmlNode infoCard = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Information']/ancestor::div[@class='card']");
        if (infoCard != null) {
            HtmlNodeCollection rows = infoCard.SelectNodes(".//table/tr");
            if (rows != null) {
                foreach (HtmlNode row in rows) {
                    HtmlNode th = row.SelectSingleNode(".//th");
                    HtmlNode td = row.SelectSingleNode(".//td");
                    if (th != null && td != null) {
                        string key = th.InnerText.Trim().TrimEnd(':');
                        // Value is in title attribute
                        string value = td.GetAttributeValue("title", td.InnerText.Trim()).Trim();

                        switch (key) {
                            case "Parser":
                                report.Summary.Parser = value;
                                break;
                            case "Assemblies":
                                report.Summary.AssemblyCount = int.Parse(value);
                                break;
                            case "Classes":
                                report.Summary.ClassCount = int.Parse(value);
                                break;
                            case "Files":
                                report.Summary.FileCount = int.Parse(value);
                                break;
                            case "Coverage date":
                                report.Summary.CoverageDate = value;
                                break;
                        }
                    }
                }
            }
        }

        // Line coverage
        HtmlNode lineCard = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Line coverage']/ancestor::div[@class='card']");
        if (lineCard != null) {
            report.Summary.LineCoverage = ParserUtils.ParseCoverageStatsFromCard(lineCard);
        }

        // Branch coverage
        HtmlNode branchCard = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Branch coverage']/ancestor::div[@class='card']");
        if (branchCard != null) {
            report.Summary.BranchCoverage = ParserUtils.ParseCoverageStatsFromCard(branchCard);
        }
    }

    /// <summary>
    /// Parses the risk hotspots table from the index.html document.
    /// </summary>
    /// <param name="doc">The parsed HTML document</param>
    /// <param name="report">CoverageReport to populate with risk hotspot data</param>
    private static void ParseRiskHotspots(HtmlDocument doc, CoverageReport report) {
        HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//risk-hotspots//table//tbody//tr");
        if (rows == null) return;

        foreach (HtmlNode row in rows) {
            HtmlNodeCollection cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 5) continue;

            string assembly = cells[0].InnerText.Trim();

            HtmlNode classLink = cells[1].SelectSingleNode(".//a");
            string className = classLink?.InnerText.Trim() ?? string.Empty;

            HtmlNode methodLink = cells[2].SelectSingleNode(".//a");
            string methodName = methodLink?.InnerText.Trim() ?? string.Empty;
            // Remove trailing "..."
            if (methodName.EndsWith("..."))
                methodName = methodName[..^3];

            decimal crapScore = decimal.Parse(cells[3].InnerText.Trim(), CultureInfo.InvariantCulture);
            int complexity = int.Parse(cells[4].InnerText.Trim());

            report.RiskHotspots.Add(new RiskHotspot {
                Assembly = assembly,
                Class = className,
                Method = methodName,
                CrapScore = crapScore,
                CyclomaticComplexity = complexity
            });
        }
    }

    /// <summary>
    /// Parses the coverage table from the index.html document for assembly and class data.
    /// </summary>
    /// <param name="doc">The parsed HTML document</param>
    /// <param name="report">CoverageReport to populate with assembly and class data</param>
    private static void ParseCoverageTable(HtmlDocument doc, CoverageReport report) {
        // Find the coverage table body
        HtmlNode tbody = doc.DocumentNode.SelectSingleNode("//h1[text()='Coverage']/following-sibling::*//table//tbody");
        if (tbody == null) return;

        HtmlNodeCollection rows = tbody.SelectNodes("tr");
        if (rows == null) return;

        AssemblyReport currentAssembly = null;

        foreach (HtmlNode row in rows) {
            // Assembly rows use <th> for all cells, class rows use <td>
            HtmlNodeCollection thCells = row.SelectNodes("th");
            HtmlNodeCollection tdCells = row.SelectNodes("td");

            if (thCells != null && thCells.Count >= 11) {
                // Assembly row
                string name = thCells[0].InnerText.Trim();
                currentAssembly = new AssemblyReport { Name = name };
                report.Assemblies.Add(currentAssembly);

                // Parse assembly-level line coverage (values in title attribute)
                string lineTitle = thCells[5].GetAttributeValue("title", "0/0");
                string[] lineParts = lineTitle.Split('/');
                int lineCovered = lineParts.Length > 0 ? int.Parse(lineParts[0]) : 0;
                int lineCoverable = lineParts.Length > 1 ? int.Parse(lineParts[1]) : 0;
                int lineTotal = int.Parse(thCells[4].InnerText.Trim());
                decimal linePercent = ParserUtils.ParsePercentage(thCells[5].InnerText.Trim());

                currentAssembly.LineCoverage = new CoverageStats {
                    Covered = lineCovered,
                    Uncovered = lineCoverable - lineCovered,
                    Coverable = lineCoverable,
                    Total = lineTotal,
                    Percentage = linePercent
                };

                // Parse assembly-level branch coverage
                string branchTitle = thCells[9].GetAttributeValue("title", "0/0");
                string[] branchParts = branchTitle.Split('/');
                int branchCovered = branchParts.Length > 0 ? int.Parse(branchParts[0]) : 0;
                int branchTotal = branchParts.Length > 1 ? int.Parse(branchParts[1]) : 0;
                decimal branchPercent = ParserUtils.ParsePercentage(thCells[9].InnerText.Trim());

                currentAssembly.BranchCoverage = new CoverageStats {
                    Covered = branchCovered,
                    Uncovered = branchTotal - branchCovered,
                    Coverable = branchTotal,
                    Percentage = branchPercent
                };
            } else if (tdCells != null && tdCells.Count >= 11) {
                // Class row
                if (currentAssembly == null) continue;

                // Parse line coverage from title attribute
                string lineTitle = tdCells[5].GetAttributeValue("title", "0/0");
                string[] lineParts = lineTitle.Split('/');
                int lineCovered = lineParts.Length > 0 ? int.Parse(lineParts[0]) : 0;
                int lineCoverable = lineParts.Length > 1 ? int.Parse(lineParts[1]) : 0;
                int lineTotal = int.Parse(tdCells[4].InnerText.Trim());
                decimal linePercent = ParserUtils.ParsePercentage(tdCells[5].InnerText.Trim());

                CoverageStats lineCoverage = new CoverageStats {
                    Covered = lineCovered,
                    Uncovered = lineCoverable - lineCovered,
                    Coverable = lineCoverable,
                    Total = lineTotal,
                    Percentage = linePercent
                };

                // Parse branch coverage from title attribute
                string branchTitle = tdCells[9].GetAttributeValue("title", "-");
                int branchCovered = 0;
                int branchTotal = 0;
                if (branchTitle != "-" && !string.IsNullOrEmpty(branchTitle)) {
                    string[] branchParts = branchTitle.Split('/');
                    branchCovered = branchParts.Length > 0 ? int.Parse(branchParts[0]) : 0;
                    branchTotal = branchParts.Length > 1 ? int.Parse(branchParts[1]) : 0;
                }
                decimal branchPercent = ParserUtils.ParsePercentage(tdCells[9].InnerText.Trim());

                CoverageStats branchCoverage = new CoverageStats {
                    Covered = branchCovered,
                    Uncovered = branchTotal - branchCovered,
                    Coverable = branchTotal,
                    Percentage = branchPercent
                };

                ClassReport classReport = new ClassReport {
                    LineCoverage = lineCoverage,
                    BranchCoverage = branchCoverage
                };

                // Get full name from the link
                HtmlNode link = tdCells[0].SelectSingleNode(".//a");
                if (link != null) {
                    classReport.FullName = link.InnerText.Trim();
                    classReport.Assembly = currentAssembly.Name;
                }

                currentAssembly.Classes.Add(classReport);
            }
        }
    }
}
