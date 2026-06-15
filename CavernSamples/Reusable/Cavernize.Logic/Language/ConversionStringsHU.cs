namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of the conversion process in Hungarian.
/// </summary>
public class ConversionStringsHU : ConversionStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("ErIRo", "A gyökérfájl érvénytelen. Kell, hogy legyen kiterjesztése.");
        Set("ErCFo", "A(z) {0} csatornához nem lehetett konvolúciós EQ-t találni az exportban ({1}).");
    }
}
