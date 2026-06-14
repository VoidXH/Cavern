namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of the conversion process in Chinese.
/// </summary>
public class ConversionStringsZH : ConversionStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("ErIRo", "根文件无效，必须具有扩展名。");
        Set("ErCFo", "在导出中未找到 {0} 声道的卷积EQ文件（{1}）。");
    }
}
