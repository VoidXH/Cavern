namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of external renderers in Hungarian.
/// </summary>
public class ExternalConverterStringsHU : ExternalConverterStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("LicNe", "A Cavernize {0}-t használ {1} fájlok konvertálásához. Automatikusan le lesz töltve, de előtte el kell fogadnod a licenszének feltételeit.");
        Set("LicFe", "Licensz letöltése...");
        Set("LicFa", "Nem sikerült a(z) {0} licensz letöltése. Ez valószínűleg egy hálózati hiba.");
        Set("LisWa", "Várakozás felhasználói beleegyezésre...");
        Set("LicCa", "A licensz nem lett elfogadva.");
        Set("PrgDl", "A(z) {0} letöltése...");
        Set("PrgEx", "A(z) {0} kicsomagolása...");
        Set("PrgRB", "Nyers bitstream elválasztása...");
        Set("PrgCo", "Konvertálás a(z) {0} segítségével...");
        Set("ErrNe", "A letöltés egy hálózati hiba miatt sikertelen volt.");
    }
}
