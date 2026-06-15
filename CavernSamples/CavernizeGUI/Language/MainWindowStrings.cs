using VoidX.WPF.Language;

namespace CavernizeGUI.Language;

/// <summary>
/// Strings used in the main window UI.
/// </summary>
public class MainWindowStrings() : LanguageBase<MainWindowStrings>(new() {
    ["MenuR"] = "Rendering",
    ["Upmix"] = "_Upmixing setup...",
    ["LoadV"] = "Load HRTF/HRIR sets for the _Virtualizer",
    ["SpVir"] = "_Height virtualization on speakers",
    ["FiltH"] = "Apply output _filters",
    ["FiltT"] = "Load a set of convolution EQs exported from QuickEQ for the target system to be used as a burnt-in room correction in the converted content.",
    ["MuBeH"] = "Mute _bed",
    ["MuBeT"] = "Only render objects that are not fixed in position, but move around.",
    ["MuGrH"] = "Mute _ground",
    ["MuGrT"] = "Any sound that only comes from the ground speakers will be muted.",
    ["For24"] = "Force 24-_bit PCM (slower, larger files, no perceived improvement)",
    ["SuSwa"] = "S_wap side/rear output channels",
    ["WavCh"] = "Skip RIFF WAVE channel mask (bypass restrictions)",
    ["SMetH"] = "Show metadata...",
    ["SMetT"] = "Display the decoded codec-specific fields of the selected track.",
    ["ReMoH"] = "_Report only mode",
    ["ReMoT"] = "To only check if a content is truly object-based or not, enable this option, and the post-render report will be available after rendering " +
    "without writing anything to the disk.",
    ["DeGrH"] = "Quality analysis and grading",
    ["DeGrT"] = "Measure perceived audio quality metrics and grade the content. This information is displayed in the post-render report.",
    ["PReSh"] = "_Show post-render report...",
    ["PReRe"] = "Post-render report",
    ["MenuH"] = "Help",
    ["UsrGu"] = "_User guide",
    ["About"] = "_About",
    ["MenuL"] = "Language",
    ["LanEn"] = "English",
    ["LanHu"] = "Magyar",
    ["LanZh"] = "简体中文",
    ["SySet"] = "System",
    ["RSInf"] = "Choose a layout, and place your speakers accordingly. Click the \"Display wiring\" button to see which output will change to which actual " +
    "channel. For maximum audio quality, calibrate your system with QuickEQ.",
    ["RndTg"] = "Render target:",
    ["DisWi"] = "Display wiring",
    ["FFLoc"] = "Locate FFmpeg",
    ["FFDes"] = "Locate FFmpeg with this button. Download it and select ffmpeg.exe in the download's bin folder. Cavernize will use FFmpeg for re-encoding. " +
    "This temporary workaround uses a lot of space, because of which, 10 GB of free space is recommended for a 2-hour movie.",
    ["CoPro"] = "Content",
    ["OpCnt"] = "Open",
    ["OpTrk"] = "Track:",
    ["OpOut"] = "Output:",
    ["OpRnd"] = "Render",
    ["ChkUp"] = "Automatically check for updates",
    ["ChkTt"] = "Periodically auto-check if a new update is available.",
    ["Queue"] = "Queue",
    ["QuDes"] = "Jobs can be queued after each other instead of manually rendering each one. Click the \"Add to queue\" button to add a set up conversion to " +
    "this queue, and click \"Process queue\" to start rendering the queued contents.",
    ["QuAdd"] = "Add to queue",
    ["QuRem"] = "Remove selected",
    ["QuSta"] = "Process",
    ["dnErr"] = "Your .NET 6 installation is corrupted and can't be loaded. Please install the latest version from Microsoft.\nPress any key to exit...",
    ["DropF"] = "Multiple files at once can only be dropped on the Queue. Drag out the right side of the window to make it visible.",
    ["IrErr"] = "The impulse response file is invalid. Error: {0}",
    ["ImFmt"] = "All supported formats|{0}|Movies|*.mkv;*.mka;*.mov;*.mp4;*.qt;*.webm;*.weba|(Enhanced) AC-3|*.ac3;*.eac3;*.ec3|Core Audio Format|*.caf|" +
    "Dolby Atmos Master Format|*.atmos|RIFF WAVE, ADM BWF|*.wav|Limitless Audio Format|*.laf",
    ["OpRun"] = "An operation is already running, please wait for it to finish.",
    ["OpRes"] = "The changes will take effect after restarting Cavernize.",
    ["LdSrc"] = "Please load a source file with at least 1 supported track for rendering.",
    ["UnTrk"] = "The selected track is not supported for rendering.",
    ["ChCnt"] = "The render target has a larger channel count ({0}) than what the selected format can handle ({1}). Please choose a different output format or " +
    "set a render target with less channels.",
    ["UnExt"] = "The file name had an unsupported extension.",
    ["UnCod"] = "This codec is not supported for export.",
    ["Start"] = "Starting render...",
    ["ExpOk"] = "Finished!",
    ["Error"] = "Error",
    ["FiltI"] = "Impulse response packages|*.wav",
    ["FiltF"] = "Cavern QuickEQ Convolution EQs|*.txt",
    ["FiltC"] = "Conflicting sample rates: headphone virtualization and output filtering can only work together if their sample rates match. One of them has to go.",
    ["FFRea"] = "Ready!",
    ["FFNRe"] = "FFmpeg isn't found, codec limitations are applied.",
    ["ProgP"] = "Rendering... ({0}, speed: {1}x, remaining: {2})",
    ["FinaP"] = "Finalizing... ({0})",
    ["DropI"] = "The following dropped files were not supported or corrupt:",
    ["AbouH"] = "About",
    ["AbouA"] = "Performance accelerated with CavernAmp.",
    ["ReQOp"] = "Can't remove elements from a queue that is already being processed.",
    ["ReQSe"] = "No queued item is selected.",
    ["FFOnl"] = "The selected output format requires FFmpeg. Please locate it with the \"Locate FFmpeg\" button, and if you need help, " +
    "click the Help/User guide button.",
    ["CMetT"] = "Codec metadata",
    ["CMeET"] = "Please load a file first.",
    ["CMeUT"] = "The Cavern API does not yet support displaying the metadata of the selected track.",
    ["JocWa"] = "This file had some parts which were coded in sparse JOC. This part of the E-AC-3 standard is not correctly documented, thus muted sections " +
    "in the audio are expected.",
    ["RenEr"] = "The audio at {0} could not be rendered. Processing will continue, but the track won't be intact after that timestamp. The exact error: {1}",
    ["SpViE"] = "The active layout does not support height virtualization on speakers. Either disable the option or choose a layout with ground channels only.",
    ["QuAlT"] = "Combined processing",
    ["QuAll"] = "Do you want to select a single output folder for all the files you're adding to the queue? If you press Yes, all of them will be processed to " +
    "their default containers in the selected folder. If you press No, you can choose the output folders, file names, and container formats individually.",
}) {
    /// <inheritdoc/>
    protected override LanguageBase<MainWindowStrings>[] GetTranslations() => [new MainWindowStringsHU(), new MainWindowStringsZH()];
}
