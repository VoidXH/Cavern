using Cavernize.Logic.Language;

namespace CavernizeAvalonia.Language;

/// <summary>
/// Reads the <see cref="RenderReportStrings"/> from Cavernize Avalonia's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="RenderReportStrings"/>.</param>
public sealed class DynamicRenderReportStrings(IReadOnlyDictionary<string, string> source) : RenderReportStrings {
    /// <inheritdoc/>
    public override string Default => source["Defau"];

    /// <inheritdoc/>
    public override string ActualBeds => source["ABeds"];

    /// <inheritdoc/>
    public override string ActualObjects => source["AObjs"];

    /// <inheritdoc/>
    public override string FakeTargets => source["FakeT"];

    /// <inheritdoc/>
    public override string PeakGain => source["PeaGa"];

    /// <inheritdoc/>
    public override string RMSGain => source["RMSGa"];

    /// <inheritdoc/>
    public override string Macrodynamics => source["MacDy"];

    /// <inheritdoc/>
    public override string Microdynamics => source["MicDy"];

    /// <inheritdoc/>
    public override string NoLFE => source["NoLFE"];

    /// <inheritdoc/>
    public override string LFEPeak => source["PeaLF"];

    /// <inheritdoc/>
    public override string LFERMS => source["RMSLF"];

    /// <inheritdoc/>
    public override string LFEMacrodynamics => source["MacLF"];

    /// <inheritdoc/>
    public override string LFEMicrodynamics => source["MicLF"];

    /// <inheritdoc/>
    public override string ChestSlam => source["CheSl"];

    /// <inheritdoc/>
    public override string SurroundUsage => source["SurUs"];

    /// <inheritdoc/>
    public override string HeightUsage => source["HeiUs"];

    /// <inheritdoc/>
    protected override string[] GetGrades() => [
        source["Grad0"],
        source["Grad1"],
        source["Grad2"],
        source["Grad3"],
        source["Grad4"],
        source["Grad5"]
    ];
}
