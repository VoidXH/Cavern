using System.Windows;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="ConversionStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="ConversionStrings"/></param>
public class DynamicConversionStrings(ResourceDictionary source) : ConversionStrings {
    /// <inheritdoc/>
    public override string InvalidRootFile => (string)source["InvRo"];

    /// <inheritdoc/>
    public override string ChannelFilterNotFound => (string)source["FiltN"];
}
