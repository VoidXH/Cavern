namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the render target selector UI in Hungarian.
/// </summary>
public class RenderTargetSelectorStringsHU : RenderTargetSelectorStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "hu-HU";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("PCRea", "Minden rendszerrel kompatibilis elrendezések");
        Set("Matri", "Különlegesen kábelezett (mátrixolt) kompatibilis elrendezések");
        Set("MulCH", "8+ csatornás elrendezések, amikhez különleges rendszer kell");
    }
}
