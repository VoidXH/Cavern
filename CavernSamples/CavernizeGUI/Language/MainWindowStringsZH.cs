namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the main window UI in Chinese.
/// </summary>
public class MainWindowStringsZH : MainWindowStrings {
    /// <inheritdoc/>
    protected override string CultureCode => "zh-CN";

    /// <inheritdoc/>
    protected override void ApplyTranslation() {
        Set("MenuR", "渲染");
        Set("Upmix", "_上混设置...");
        Set("LoadV", "为_虚拟器加载HRTF/HRIR集");
        Set("SpVir", "在扬声器上进行_高度虚拟化");
        Set("FiltH", "应用输出_滤波器");
        Set("FiltT", "加载一组从QuickEQ导出的卷积均衡器，用于目标系统，作为转换内容中的内置房间校正。");
        Set("MuBeH", "静音_基础声道");
        Set("MuBeT", "仅渲染位置不固定、会移动的对象。");
        Set("MuGrH", "静音_地面声道");
        Set("MuGrT", "仅来自地面扬声器的任何声音将被静音。");
        Set("For24", "强制24_位PCM（更慢、文件更大、无明显听觉改善）");
        Set("SuSwa", "_交换侧/后输出声道");
        Set("WavCh", "跳过RIFF WAVE声道掩码（绕过限制）");
        Set("SMetH", "显示元数据...");
        Set("SMetT", "显示所选轨道解码后编解码器特定的字段。");
        Set("ReMoH", "_仅报告模式");
        Set("ReMoT", "若仅检查内容是否真正基于对象，启用此选项后，渲染后报告将可用，而无需写入磁盘。");
        Set("DeGrH", "质量分析与评分");
        Set("DeGrT", "测量感知音频质量指标并对内容进行评分。此信息将显示在渲染后报告中。");
        Set("PReSh", "_显示渲染后报告...");
        Set("PReRe", "渲染后报告");
        Set("MenuH", "帮助");
        Set("UsrGu", "_用户指南");
        Set("About", "_关于");
        Set("MenuL", "语言(_L)");
        Set("SySet", "系统");
        Set("RSInf", "选择一个布局，并相应地放置扬声器。点击\"显示接线\"按钮查看哪个输出对应哪个实际声道。" +
            "为获得最佳音频质量，请使用QuickEQ校准您的系统。");
        Set("RndTg", "渲染目标：");
        Set("DisWi", "显示接线");
        Set("FFLoc", "定位FFmpeg");
        Set("FFDes", "使用此按钮定位FFmpeg。下载FFmpeg并在下载目录的bin文件夹中选择ffmpeg.exe。" +
            "Cavernize将使用FFmpeg进行重新编码。此临时解决方法占用大量空间，因此对于2小时的电影，建议至少预留10 GB可用空间。");
        Set("CoPro", "内容");
        Set("OpCnt", "打开");
        Set("OpTrk", "轨道：");
        Set("OpOut", "输出：");
        Set("OpRnd", "渲染");
        Set("ChkUp", "自动检查更新");
        Set("ChkTt", "定期自动检查是否有新更新可用。");
        Set("Queue", "队列");
        Set("QuDes", "作业可以依次排队处理，而无需手动渲染每个作业。" +
            "点击\"添加到队列\"按钮将配置好的转换加入队列，点击\"处理\"按钮开始渲染队列中的内容。");
        Set("QuAdd", "添加到队列");
        Set("QuRem", "删除选中项");
        Set("QuSta", "处理");
        Set("dnErr", "您的.NET 6安装已损坏，无法加载。请从Microsoft安装最新版本。\n按任意键退出...");
        Set("DropF", "一次只能将多个文件拖放到队列上。向右拖动窗口边缘使其可见。");
        Set("IrErr", "脉冲响应文件无效。错误：{0}");
        Set("ImFmt", "所有支持的格式|{0}|电影|*.mkv;*.mka;*.mov;*.mp4;*.qt;*.webm;*.weba|(Enhanced) AC-3|*.ac3;*.eac3;*.ec3|" +
            "Core Audio Format|*.caf|Dolby Atmos Master Format|*.atmos|RIFF WAVE, ADM BWF|*.wav|Limitless Audio Format|*.laf");
        Set("OpRun", "已有操作正在运行，请等待其完成。");
        Set("OpRes", "更改将在重新启动Cavernize后生效。");
        Set("LdSrc", "请加载一个至少包含1条支持渲染轨道的源文件。");
        Set("UnTrk", "所选轨道不支持渲染。");
        Set("ChCnt", "渲染目标的声道数（{0}）超过了所选格式能够处理的声道数（{1}）。" +
            "请选择不同的输出格式或设置声道数更少的渲染目标。");
        Set("UnExt", "文件名的扩展名不受支持。");
        Set("UnCod", "此编解码器不支持导出。");
        Set("Start", "开始渲染...");
        Set("ExpOk", "完成！");
        Set("Error", "错误");
        Set("FiltI", "脉冲响应包|*.wav");
        Set("FiltF", "Cavern QuickEQ卷积均衡器|*.txt");
        Set("FiltC", "采样率冲突：耳机虚拟化和输出滤波只有在采样率匹配时才能同时工作。必须去掉一个。");
        Set("FFRea", "就绪！");
        Set("FFNRe", "未找到FFmpeg，应用编解码器限制。");
        Set("ProgP", "渲染中...（{0}，速度：{1}x，剩余：{2}）");
        Set("FinaP", "最终处理中...（{0}）");
        Set("DropI", "以下拖放的文件不受支持或已损坏：");
        Set("AbouH", "关于");
        Set("AbouA", "性能由CavernAmp加速。");
        Set("ReQOp", "无法从正在处理的队列中删除元素。");
        Set("ReQSe", "未选中队列中的项目。");
        Set("FFOnl", "所选输出格式需要FFmpeg。请使用\"定位FFmpeg\"按钮定位它，如需帮助，请点击帮助/用户指南按钮。");
        Set("CMetT", "编解码器元数据");
        Set("CMeET", "请先加载文件。");
        Set("CMeUT", "Cavern API尚不支持显示所选轨道的元数据。");
        Set("JocWa", "此文件的某些部分是用稀疏JOC编码的。E-AC-3标准的这一部分没有正确文档化，因此音频中可能出现静音片段。");
        Set("RenEr", "{0}处的音频无法渲染。处理将继续，但该轨道在此时间戳之后将不完整。具体错误：{1}");
        Set("SpViE", "当前布局不支持在扬声器上进行高度虚拟化。请禁用此选项或选择仅包含地面声道的布局。");
        Set("QuAlT", "组合处理");
        Set("QuAll", "是否要为添加到队列中的所有文件选择一个统一的输出文件夹？" +
            "如果按\"是\"，所有文件将在所选文件夹中以其默认容器格式处理。" +
            "如果按\"否\"，您可以逐个选择输出文件夹、文件名和容器格式。");
    }
}
