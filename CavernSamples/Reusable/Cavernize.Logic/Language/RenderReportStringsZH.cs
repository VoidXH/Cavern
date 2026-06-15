namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used for generating a post-render report in Chinese.
/// </summary>
public class RenderReportStringsZH : RenderReportStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("Defau", "渲染完成后，您可以在此处查看额外的轨道信息，例如实际对象使用统计。");
        Set("ABeds", "实际包含的基础声道");
        Set("AObjs", "实际包含的动态对象");
        Set("FakeT", "未使用（虚假）渲染目标");
        Set("PeaGa", "峰值音频帧电平");
        Set("RMSGa", "内容RMS电平");
        Set("MacDy", "宏观动态");
        Set("MicDy", "微观动态");
        Set("NoLFE", "源中缺少LFE声道、未使用或未被渲染。");
        Set("PeaLF", "峰值LFE电平");
        Set("RMSLF", "RMS LFE电平");
        Set("MacLF", "LFE宏观动态");
        Set("MicLF", "LFE微观动态");
        Set("CheSl", "捶胸感等级");
        Set("SurUs", "环绕声使用率");
        Set("HeiUs", "高度使用率");
        Set("Grad0", "5*");
        Set("Grad1", "5");
        Set("Grad2", "4");
        Set("Grad3", "3");
        Set("Grad4", "2");
        Set("Grad5", "1");
    }
}
