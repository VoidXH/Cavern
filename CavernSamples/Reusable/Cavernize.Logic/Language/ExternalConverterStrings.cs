using VoidX.WPF.Language;

namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of external renderers.
/// </summary>
public class ExternalConverterStrings() : LanguageBase<ExternalConverterStrings>(new() {
    ["LicNe"] = "Cavernize uses {0} for {1} conversions. It will be downloaded automatically, but you need to accept its licence agreement first.",
    ["LicFe"] = "Fetching licence...",
    ["LicFa"] = "Failed to fetch {0} licence. This is probably a network error.",
    ["LisWa"] = "Waiting for user approval...",
    ["LicCa"] = "The licence was not accepted.",
    ["PrgDl"] = "Downloading {0}...",
    ["PrgEx"] = "Extracting {0}...",
    ["PrgRB"] = "Extracting raw bitstream...",
    ["PrgCo"] = "Converting with {0}...",
    ["ErrNe"] = "Downloading failed because of a network error.",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<ExternalConverterStrings>[] GetTranslations() => [new ExternalConverterStringsHU(), new ExternalConverterStringsZH()];
}
