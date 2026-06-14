using VoidX.WPF.Language;

namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used for generating a post-render report.
/// </summary>
public class RenderReportStrings() : LanguageBase<RenderReportStrings>(new() {
    ["Defau"] = "After rendering has finished, more track information will appear here, like true object usage statistics.",
    ["ABeds"] = "Actually present bed channels",
    ["AObjs"] = "Actually present dynamic objects",
    ["FakeT"] = "Unused (fake) rendering targets",
    ["PeaGa"] = "Peak audio frame level",
    ["RMSGa"] = "RMS content level",
    ["MacDy"] = "Macrodynamics",
    ["MicDy"] = "Microdynamics",
    ["NoLFE"] = "The LFE channel was either missing from the source, unused, or not rendered.",
    ["PeaLF"] = "Peak LFE level",
    ["RMSLF"] = "RMS LFE level",
    ["MacLF"] = "LFE macrodynamics",
    ["MicLF"] = "LFE microdynamics",
    ["CheSl"] = "Chest slam grade",
    ["SurUs"] = "Surround usage",
    ["HeiUs"] = "Height usage",
    ["Grad0"] = "A+",
    ["Grad1"] = "A",
    ["Grad2"] = "B",
    ["Grad3"] = "C",
    ["Grad4"] = "D",
    ["Grad5"] = "F",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<RenderReportStrings>[] GetTranslations() => [new RenderReportStringsHU()];

    /// <summary>
    /// 6 grades from best to worst.
    /// </summary>
    public string[] Grades => grades ??= [this["Grad0"], this["Grad1"], this["Grad2"], this["Grad3"], this["Grad4"], this["Grad5"]];
    string[] grades;
}
