namespace Cavernize.Logic.Language;

/// <summary>
/// Strings for the messages of external renderers in Chinese.
/// </summary>
public class ExternalConverterStringsZH : ExternalConverterStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("LicNe", "Cavernize使用 {0} 进行 {1} 转换。它将自动下载，但您需要先接受其许可协议。");
        Set("LicFe", "正在获取许可协议...");
        Set("LicFa", "无法获取 {0} 许可协议，可能是网络错误。");
        Set("LisWa", "等待用户同意...");
        Set("LicCa", "许可协议未被接受。");
        Set("PrgDl", "正在下载 {0}...");
        Set("PrgEx", "正在解压 {0}...");
        Set("PrgRB", "正在提取原始比特流...");
        Set("PrgCo", "正在使用 {0} 转换...");
        Set("ErrNe", "下载失败，网络错误。");
    }
}
