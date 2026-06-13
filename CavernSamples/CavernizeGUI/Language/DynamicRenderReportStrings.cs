using System.Windows;

using Cavern.WPF.Utils;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="RenderReportStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="RenderReportStrings"/></param>
public class DynamicRenderReportStrings(ResourceDictionary source) : RenderReportStrings {
    /// <inheritdoc/>
    public override string Default => source.TryGet("Defau", base.Default);

    /// <inheritdoc/>
    public override string ActualBeds => source.TryGet("ABeds", base.ActualBeds);

    /// <inheritdoc/>
    public override string ActualObjects => source.TryGet("AObjs", base.ActualObjects);

    /// <inheritdoc/>
    public override string FakeTargets => source.TryGet("FakeT", base.FakeTargets);

    /// <inheritdoc/>
    public override string PeakGain => source.TryGet("PeaGa", base.PeakGain);

    /// <inheritdoc/>
    public override string RMSGain => source.TryGet("RMSGa", base.RMSGain);

    /// <inheritdoc/>
    public override string Macrodynamics => source.TryGet("MacDy", base.Macrodynamics);

    /// <inheritdoc/>
    public override string Microdynamics => source.TryGet("MicDy", base.Microdynamics);

    /// <inheritdoc/>
    public override string NoLFE => source.TryGet("NoLFE", base.NoLFE);

    /// <inheritdoc/>
    public override string LFEPeak => source.TryGet("PeaLF", base.LFEPeak);

    /// <inheritdoc/>
    public override string LFERMS => source.TryGet("RMSLF", base.LFERMS);

    /// <inheritdoc/>
    public override string LFEMacrodynamics => source.TryGet("MacLF", base.LFEMacrodynamics);

    /// <inheritdoc/>
    public override string LFEMicrodynamics => source.TryGet("MicLF", base.LFEMicrodynamics);

    /// <inheritdoc/>
    public override string ChestSlam => source.TryGet("CheSl", base.ChestSlam);

    /// <inheritdoc/>
    public override string SurroundUsage => source.TryGet("SurUs", base.SurroundUsage);

    /// <inheritdoc/>
    public override string HeightUsage => source.TryGet("HeiUs", base.HeightUsage);

    /// <inheritdoc/>
    protected override string[] GetGrades() => [
        (string)source["Grad0"],
        (string)source["Grad1"],
        (string)source["Grad2"],
        (string)source["Grad3"],
        (string)source["Grad4"],
        (string)source["Grad5"]
    ];
}
