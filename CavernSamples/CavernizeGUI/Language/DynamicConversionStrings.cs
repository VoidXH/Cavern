using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="ConversionStrings"/> from Cavernize Avalonia's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="ConversionStrings"/>.</param>
public sealed class DynamicConversionStrings(IReadOnlyDictionary<string, string> source) : ConversionStrings {
    /// <inheritdoc/>
    public override string InvalidRootFile => source["InvRo"];

    /// <inheritdoc/>
    public override string ChannelFilterNotFound => source["FiltN"];
}
