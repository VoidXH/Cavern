using Cavernize.Logic.Language;

namespace CavernizeAvalonia.Language;

/// <summary>
/// Reads the <see cref="ExternalConverterStrings"/> from Cavernize Avalonia's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="ExternalConverterStrings"/>.</param>
public sealed class DynamicExternalConverterStrings(IReadOnlyDictionary<string, string> source) : ExternalConverterStrings {
    /// <inheritdoc/>
    public override string LicenceNeeded => source["LicNe"];

    /// <inheritdoc/>
    public override string LicenceFetch => source["LicFe"];

    /// <inheritdoc/>
    public override string LicenceFail => source["LicFa"];

    /// <inheritdoc/>
    public override string WaitingUserAccept => source["LicWa"];

    /// <inheritdoc/>
    public override string UserCancelled => source["LicCa"];

    /// <inheritdoc/>
    public override string Downloading => source["ExDow"];

    /// <inheritdoc/>
    public override string Extracting => source["ExExt"];

    /// <inheritdoc/>
    public override string ExtractingBitstream => source["ExRaw"];

    /// <inheritdoc/>
    public override string Converting => source["ConvW"];

    /// <inheritdoc/>
    public override string NetworkError => source["DlErr"];
}
