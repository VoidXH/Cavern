using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

using CoverageParser.Models;

namespace CoverageParser.Parsers;

/// <summary>
/// Parses individual class HTML files from a coverage report directory to extract method-level details.
/// </summary>
public static class ClassHtmlParser {
    /// <summary>
    /// Parses all class HTML files (Cavern_*.html) in the report directory and populates the coverage report.
    /// </summary>
    /// <param name="reportDirectory">Path to the directory containing the class HTML files</param>
    /// <param name="report">CoverageReport to populate with method-level coverage data</param>
    public static void ParseClassHtmlFiles(string reportDirectory, CoverageReport report) {
        string[] classFiles = Directory.GetFiles(reportDirectory, "Cavern_*.html");

        foreach (string classFile in classFiles) {
            ParseSingleClassHtml(classFile, report);
        }
    }

    /// <summary>
    /// Parses a single class HTML file and populates the coverage report with method-level details.
    /// </summary>
    /// <param name="classFile">Path to the class HTML file</param>
    /// <param name="report">CoverageReport to populate with method-level coverage data</param>
    public static void ParseSingleClassHtml(string classFile, CoverageReport report) {
        HtmlDocument doc = ParserUtils.LoadHtml(classFile);

        // Get class name from the title or info table
        HtmlNode classInfo = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Information']/ancestor::div[@class='card']");
        if (classInfo == null) {
            return;
        }

        HtmlNodeCollection classRows = classInfo.SelectNodes(".//table/tr");
        if (classRows == null) {
            return;
        }

        string className = string.Empty;
        string assemblyName = string.Empty;
        string filePath = string.Empty;

        foreach (HtmlNode row in classRows) {
            HtmlNode th = row.SelectSingleNode(".//th");
            HtmlNode td = row.SelectSingleNode(".//td");
            if (th != null && td != null) {
                string key = th.InnerText.Trim().TrimEnd(':');
                string value = td.InnerText.Trim();

                switch (key) {
                    case "Class":
                        className = value;
                        break;
                    case "Assembly":
                        assemblyName = value;
                        break;
                    case "File(s)":
                        HtmlNode link = td.SelectSingleNode(".//a");
                        filePath = link?.GetAttributeValue("href", string.Empty);
                        break;
                }
            }
        }

        // Find matching class report in assemblies
        ClassReport matchingClass = ParserUtils.FindClassReport(report, assemblyName, className);
        if (matchingClass == null) {
            // Create a new one if not found in index.html
            matchingClass = new ClassReport {
                FullName = className,
                Assembly = assemblyName,
                FilePath = filePath
            };

            AssemblyReport assembly = report.Assemblies.FirstOrDefault(a => a.Name == assemblyName);
            assembly?.Classes.Add(matchingClass);
        }

        matchingClass.FilePath = filePath;

        // Parse line coverage
        HtmlNode lineCard = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Line coverage']/ancestor::div[@class='card']");
        if (lineCard != null) {
            matchingClass.LineCoverage = ParserUtils.ParseCoverageStatsFromCard(lineCard);
        }

        // Parse branch coverage
        HtmlNode branchCard = doc.DocumentNode.SelectSingleNode("//div[@class='card-header' and text()='Branch coverage']/ancestor::div[@class='card']");
        if (branchCard != null) {
            matchingClass.BranchCoverage = ParserUtils.ParseCoverageStatsFromCard(branchCard);
        }

        // Parse method coverage table
        ParseMethodCoverage(doc, matchingClass);
    }

    /// <summary>
    /// Parses the method coverage table from a class HTML document.
    /// </summary>
    /// <param name="doc">The parsed HTML document</param>
    /// <param name="classReport">ClassReport to populate with method coverage data</param>
    private static void ParseMethodCoverage(HtmlDocument doc, ClassReport classReport) {
        HtmlNode tbody = doc.DocumentNode.SelectSingleNode("//h1[text()='Metrics']/following-sibling::div//table//tbody");
        if (tbody == null) {
            return;
        }

        HtmlNodeCollection rows = tbody.SelectNodes("tr");
        if (rows == null) {
            return;
        }

        foreach (HtmlNode row in rows) {
            HtmlNodeCollection cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 5) {
                continue;
            }

            // Method name from the first cell
            HtmlNode methodLink = cells[0].SelectSingleNode(".//a");
            string methodName = methodLink?.InnerText.Trim() ?? string.Empty;

            // Line number from the anchor href (e.g., "file0_line15")
            int line = 0;
            if (methodLink != null) {
                string href = methodLink.GetAttributeValue("href", string.Empty);
                Match lineMatch = Regex.Match(href, @"line(\d+)");
                if (lineMatch.Success) {
                    line = int.Parse(lineMatch.Groups[1].Value);
                }
            }

            // Branch coverage
            decimal branchPct = ParserUtils.ParsePercentage(cells[1].InnerText.Trim());

            // Crap score
            decimal crapScore = decimal.Parse(cells[2].InnerText.Trim(), CultureInfo.InvariantCulture);

            // Cyclomatic complexity
            int complexity = int.Parse(cells[3].InnerText.Trim());

            // Line coverage
            decimal linePct = ParserUtils.ParsePercentage(cells[4].InnerText.Trim());

            classReport.Methods.Add(new MethodReport {
                Name = methodName,
                Line = line,
                BranchCoverage = branchPct,
                CrapScore = crapScore,
                CyclomaticComplexity = complexity,
                LineCoverage = linePct
            });
        }
    }
}
