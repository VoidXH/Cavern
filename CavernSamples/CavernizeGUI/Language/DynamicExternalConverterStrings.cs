using System.Windows;

using Cavern.WPF.Utils;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="ExternalConverterStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="ExternalConverterStrings"/></param>
public class DynamicExternalConverterStrings(ResourceDictionary source) : ExternalConverterStrings {
    /// <inheritdoc/>
    public override string LicenceNeeded => source.TryGet("LicNe", base.LicenceNeeded);

    /// <inheritdoc/>
    public override string LicenceFetch => source.TryGet("LicFe", base.LicenceFetch);

    /// <inheritdoc/>
    public override string LicenceFail => source.TryGet("LicFa", base.LicenceFail);

    /// <inheritdoc/>
    public override string WaitingUserAccept => source.TryGet("LicWa", base.WaitingUserAccept);

    /// <inheritdoc/>
    public override string UserCancelled => source.TryGet("LicCa", base.UserCancelled);

    /// <inheritdoc/>
    public override string Downloading => source.TryGet("ExDow", base.Downloading);

    /// <inheritdoc/>
    public override string Extracting => source.TryGet("ExExt", base.Extracting);

    /// <inheritdoc/>
    public override string ExtractingBitstream => source.TryGet("ExRaw", base.ExtractingBitstream);

    /// <inheritdoc/>
    public override string Converting => source.TryGet("ConvW", base.Converting);

    /// <inheritdoc/>
    public override string NetworkError => source.TryGet("DlErr", base.NetworkError);
}
