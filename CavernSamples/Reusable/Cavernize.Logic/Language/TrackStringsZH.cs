namespace Cavernize.Logic.Language;

/// <summary>
/// Strings used in the track selection UI in Chinese.
/// </summary>
public class TrackStringsZH : TrackStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("NoSup", "Cavern不支持的音轨");
        Set("E3JOC", "Enhanced AC-3 联合对象编码");
        Set("ObTra", "基于对象的音轨");
        Set("ChTra", "基于声道的音轨");
        Set("SouCh", "源声道");
        Set("MatBe", "矩阵化基础");
        Set("MatOb", "矩阵化对象");
        Set("SouBe", "基础声道");
        Set("SouDy", "动态对象");
        Set("Chans", "声道");
        Set("WiObj", "含对象");
        Set("InvTr", "此音轨无法解码。解码时发生以下错误：");
        Set("Later", "这可能在后续版本中修复。请尝试更新Cavernize，" +
            "如果问题在最新版本中仍然存在，请在www.sbence.hu上联系开发者。");
        Set("PCMFl", "PCM（浮点）");
        Set("PCMLE", "PCM（整数）");
        Set("C_AC3", "AC-3（中等质量，支持SPDIF）");
        Set("CEAC3", "Enhanced AC-3（中等质量，支持HDMI ARC）");
        Set("COpus", "Opus（透明，体积小）");
        Set("CFLAC", "FLAC（无损，体积大）");
        Set("CPCMF", "PCM浮点（不必要，体积最大）");
        Set("CPCMI", "PCM整数（无损，体积更大）");
        Set("CADMC", "ADM Broadcast Wave Format（紧凑）");
        Set("CADMA", "ADM Broadcast Wave Format（Dolby Atmos）");
        Set("CDAMF", "Dolby Atmos Master Format");
        Set("C_LAF", "Limitless Audio Format");
    }
}
