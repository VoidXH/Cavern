using VoidX.WPF.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the render target selector UI.
/// </summary>
public class RenderTargetSelectorStrings() : LanguageBase<RenderTargetSelectorStrings>(new() {
    ["PCRea"] = "Layouts compatible with all systems",
    ["Matri"] = "Specially wired (matrixed) compatible layouts",
    ["MulCH"] = "8+ channel layouts requiring special systems",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<RenderTargetSelectorStrings>[] GetTranslations() => [new RenderTargetSelectorStringsHU()];
}
