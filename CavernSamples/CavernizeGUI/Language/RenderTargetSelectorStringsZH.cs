namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the render target selector UI in Chinese.
/// </summary>
public class RenderTargetSelectorStringsZH : RenderTargetSelectorStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("PCRea", "兼容所有系统的布局");
        Set("Matri", "特殊接线（矩阵化）的兼容布局");
        Set("MulCH", "需要特殊系统的8+声道布局");
    }
}
