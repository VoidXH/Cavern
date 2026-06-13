using System.Windows;

using Cavern.WPF.Utils;

using Cavernize.Logic.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Reads the <see cref="ConversionStrings"/> from Cavernize GUI's localized resources.
/// </summary>
/// <param name="source">Localized resources for <see cref="ConversionStrings"/></param>
public class DynamicConversionStrings(ResourceDictionary source) : ConversionStrings {
    /// <inheritdoc/>
    public override string InvalidRootFile => source.TryGet("InvRo", base.InvalidRootFile);

    /// <inheritdoc/>
    public override string ChannelFilterNotFound => source.TryGet("FiltN", base.ChannelFilterNotFound);
}
