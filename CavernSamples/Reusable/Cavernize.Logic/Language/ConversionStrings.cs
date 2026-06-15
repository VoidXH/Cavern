using VoidX.WPF.Language;

namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of the conversion process.
/// </summary>
public class ConversionStrings() : LanguageBase<ConversionStrings>(new() {
    ["ErIRo"] = "The root file was invalid. It must have an extension.",
    ["ErCFo"] = "Convolution EQ file for the {0} channel was not found in this export ({1}).",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<ConversionStrings>[] GetTranslations() => [new ConversionStringsHU(), new ConversionStringsZH()];
}
