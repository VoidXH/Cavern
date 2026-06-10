using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Avalonia.Threading;
using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.CommandLine;
using Cavernize.Logic.External;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using VoidX.WPF.FFmpeg;

using GuiLanguage = CavernizeGUI.Consts.Language;

namespace CavernizeGUI;

public partial class MainWindow : Avalonia.Controls.Window, INotifyPropertyChanged, IDisposable {
    public new event PropertyChangedEventHandler PropertyChanged;

    public ExportFormat[] ExportFormats { get; }

    public RenderTarget[] RenderTargets { get; }

    public ObservableCollection<CavernizeTrack> Tracks { get; } = [];

    public ObservableCollection<QueuedRenderJob> QueueJobs { get; } = [];

    public string LoadedPath {
        get => loadedPath;
        private set => SetProperty(ref loadedPath, value);
    }

    public string LoadedTitle {
        get => loadedTitle;
        private set => SetProperty(ref loadedTitle, value);
    }

    public CavernizeTrack SelectedTrack {
        get => selectedTrack;
        set {
            if (SetProperty(ref selectedTrack, value)) {
                UpdateTrackDetails(value);
                OnPropertyChanged(nameof(CanRender));
            }
        }
    }

    public ExportFormat SelectedExportFormat {
        get => selectedExportFormat;
        set {
            if (SetProperty(ref selectedExportFormat, value)) {
                SaveSettings();
                NotifyProperties(nameof(SuggestedOutputName), nameof(SuggestedOutputExtension));
                UpdateCommandState();
            }
        }
    }

    public RenderTarget SelectedRenderTarget {
        get => selectedRenderTarget;
        set {
            if (SetProperty(ref selectedRenderTarget, value)) {
                UpdateRenderTargetDetails();
                if (RenderingSettings.RoomCorrection != null) {
                    ClearRoomCorrectionState();
                    Status = Text("FiltC");
                    NotifyRoomCorrectionChanged();
                }
                SaveSettings();
            }
        }
    }

    public bool SpeakerVirtualizer {
        get => RenderingSettings.SpeakerVirtualizer;
        set => SetPersistedOption(RenderingSettings.SpeakerVirtualizer, value,
            newValue => RenderingSettings.SpeakerVirtualizer = newValue);
    }

    public bool Force24Bit {
        get => RenderingSettings.Force24Bit;
        set => SetPersistedOption(RenderingSettings.Force24Bit, value,
            newValue => RenderingSettings.Force24Bit = newValue);
    }

    public bool MuteBed {
        get => RenderingSettings.MuteBed;
        set => SetPersistedOption(RenderingSettings.MuteBed, value,
            newValue => RenderingSettings.MuteBed = newValue);
    }

    public bool MuteGround {
        get => RenderingSettings.MuteGround;
        set => SetPersistedOption(RenderingSettings.MuteGround, value,
            newValue => RenderingSettings.MuteGround = newValue);
    }

    public bool WavChannelSkip {
        get => settings.WavChannelSkip;
        set => SetPersistedOption(settings.WavChannelSkip, value, newValue => settings.WavChannelSkip = newValue);
    }

    public bool DetailedGrading {
        get => settings.DetailedGrading;
        set => SetPersistedOption(settings.DetailedGrading, value, newValue => settings.DetailedGrading = newValue);
    }

    public bool CheckUpdates {
        get => settings.CheckUpdates;
        set {
            if (settings.CheckUpdates != value) {
                settings.CheckUpdates = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public double ViewScale {
        get => settings.ViewScale is >= .5 and <= 1.25 ? settings.ViewScale : 1;
        set {
            double scale = Math.Clamp(value, .5, 1.25);
            if (Math.Abs(settings.ViewScale - scale) > .001) {
                settings.ViewScale = scale;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool ReportMode {
        get => reportMode;
        set {
            if (SetProperty(ref reportMode, value)) {
                OnPropertyChanged(nameof(CanAddToQueue));
            }
        }
    }

    public bool MatrixUpmixing => UpmixingSettings.MatrixUpmixing;

    public bool CavernizeUpmixing => UpmixingSettings.Cavernize;

    public float UpmixingEffect => UpmixingSettings.Effect;

    public float UpmixingSmoothness => UpmixingSettings.Smoothness;

    public QueuedRenderJob SelectedQueueJob {
        get => selectedQueueJob;
        set {
            if (SetProperty(ref selectedQueueJob, value)) {
                OnPropertyChanged(nameof(CanRemoveQueueJob));
            }
        }
    }

    public string TrackFormatHeader {
        get => trackFormatHeader;
        private set => SetProperty(ref trackFormatHeader, value);
    }

    public string TrackDetail1Title {
        get => trackDetail1Title;
        private set => SetProperty(ref trackDetail1Title, value);
    }

    public string TrackDetail1Value {
        get => trackDetail1Value;
        private set => SetProperty(ref trackDetail1Value, value);
    }

    public string TrackDetail2Title {
        get => trackDetail2Title;
        private set => SetProperty(ref trackDetail2Title, value);
    }

    public string TrackDetail2Value {
        get => trackDetail2Value;
        private set => SetProperty(ref trackDetail2Value, value);
    }

    public string TrackDetail3Title {
        get => trackDetail3Title;
        private set => SetProperty(ref trackDetail3Title, value);
    }

    public string TrackDetail3Value {
        get => trackDetail3Value;
        private set => SetProperty(ref trackDetail3Value, value);
    }

    public string ActiveChannels {
        get => activeChannels;
        private set => SetProperty(ref activeChannels, value);
    }

    public double Progress {
        get => progress;
        private set => SetProperty(ref progress, value);
    }

    public bool IsProgressIndeterminate {
        get => isProgressIndeterminate;
        private set => SetProperty(ref isProgressIndeterminate, value);
    }

    public string Status {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string Warning {
        get => warning;
        private set => SetProperty(ref warning, value);
    }

    public string ReportText {
        get => reportText;
        private set => SetProperty(ref reportText, value);
    }

    public bool IsBusy {
        get => isBusy;
        private set {
            if (SetProperty(ref isBusy, value)) {
                UpdateCommandState();
            }
        }
    }

    public bool CanUseCommands => !IsBusy;

    public bool CanRender => !IsBusy && SelectedTrack != null;

    public bool CanAddToQueue => !IsBusy && !ReportMode && !string.IsNullOrWhiteSpace(LoadedPath);

    public bool CanRunQueue => !IsBusy && QueueJobs.Count != 0;

    public bool HasQueueJobs => QueueJobs.Count != 0;

    public bool CanRemoveQueueJob => !IsBusy && SelectedQueueJob != null;

    public bool CanCancel => IsBusy;

    public string LastDirectory => settings.LastDirectory;

    public string LastFilterDirectory => settings.LastFilterDirectory;

    public string LanguageCode => language.Code;

    public string SystemTitle => Text("SySet");

    public string SystemInfoText => Text("RSInf");

    public string RenderTargetLabel => Text("RndTg");

    public string DisplayWiringText => Text("DisWi");

    public string ContentTitle => Text("CoPro");

    public string OpenText => Text("OpCnt");

    public string TrackLabel => Text("OpTrk");

    public string OutputLabel => Text("OpOut");

    public string AddToQueueText => Text("QuAdd");

    public string RenderText => Text("OpRnd");

    public string QueueTitle => Text("Queue");

    public string RemoveSelectedText => Text("QuRem");

    public string ProcessText => Text("QuSta");

    public string NoTrackLoadedText => Text("NoTrk");

    public string OpenSourcePickerTitle => Text("OpSrc");

    public string SaveRenderPickerTitle => Text("SavRn");

    public string AudioVideoFileType => Text("AudVi");

    public string SelectedFormatFileType => Text("SelFo");

    public string LoadHrirTitle => Text("LoadH");

    public string LoadFiltersTitle => Text("LoadF");

    public string ImpulseResponseFileType => FileTypeName("FiltI");

    public string RoomCorrectionFileType => FileTypeName("FiltF");

    public string NoWarningsText => Text("NoWar");

    public bool HasHrir {
        get => hasHrir;
        private set {
            if (SetProperty(ref hasHrir, value)) {
                OnPropertyChanged(nameof(HrirStatus));
            }
        }
    }

    public bool HasRoomCorrection => RenderingSettings.RoomCorrection != null;

    public string HrirStatus => HasHrir ? $"HRIR: {Path.GetFileName(settings.HrirPath)}" : "HRIR: built-in";

    public string RoomCorrectionStatus =>
        HasRoomCorrection ? $"Filters: {Path.GetFileName(roomCorrectionPath)}" : "Filters: none";

    public DateTime LastUpdateCheck => settings.LastUpdateCheck;

    public string SuggestedOutputExtension {
        get {
            string filter = SelectedExportFormat?.Codec.GetSaveDialogFilter();
            int marker = filter?.IndexOf("*.") ?? -1;
            if (marker >= 0) {
                int end = filter.IndexOfAny([';', '|'], marker);
                return filter[(marker + 2)..(end < 0 ? filter.Length : end)];
            }
            return SelectedExportFormat?.Codec == Codec.PCM_LE || SelectedExportFormat?.Codec == Codec.PCM_Float ?
                "wav" :
                "mkv";
        }
    }

    public string SuggestedOutputName {
        get {
            string stem = string.IsNullOrWhiteSpace(LoadedPath) ? "Cavernize Render" : Path.GetFileNameWithoutExtension(LoadedPath);
            return $"{stem}.{SuggestedOutputExtension}";
        }
    }

    internal FFmpeg FFmpeg => ffmpeg;

    internal GuiLanguage Language => language;

    readonly AppSettings settings = AppSettings.Load();
    readonly GuiLanguage language;
    readonly ConversionEnvironment environment;
    readonly CallbackFFmpeg ffmpeg;
    CancellationTokenSource cancellation;
    QueuedRenderJob activeJob;
    QueuedRenderJob selectedQueueJob;
    PostRenderReport report;
    string roomCorrectionPath;
    string loadedPath;
    string loadedTitle;
    CavernizeTrack selectedTrack;
    ExportFormat selectedExportFormat;
    RenderTarget selectedRenderTarget;
    double progress;
    bool isProgressIndeterminate;
    bool isBusy;
    bool rendering;
    bool hasHrir;
    bool reportMode;
    string status;
    string warning;
    string reportText;
    string trackFormatHeader;
    string trackDetail1Title;
    string trackDetail1Value;
    string trackDetail2Title;
    string trackDetail2Value;
    string trackDetail3Title;
    string trackDetail3Value;
    string activeChannels;
    int blockSize;

    public MainWindow() {
        language = GuiLanguage.Create(settings.LanguageCode);
        FFmpeg.ReadyText = Text("FFRea");
        FFmpeg.NotReadyText = Text("FFNRe");
        ffmpeg = new CallbackFFmpeg(UpdateStatus, settings.FFmpegPath);
        RenderingSettings = new DynamicSpecialRenderModeSettings();
        UpmixingSettings = new DynamicUpmixingSettings();
        loadedTitle = Text("NoSrc");
        status = Text("OpSrcS");
        warning = NoWarningsText;

        ExportFormats = ExportFormat.GetFormats(language.TrackStrings);
        RenderTargets = RenderTarget.Targets.Where(target => target is not DriverRenderTarget).ToArray();
        selectedExportFormat = ExportFormats.ElementAtOrDefault(settings.OutputCodecIndex + 2) ??
            ExportFormats.First(format => format.Codec == Codec.PCM_LE);
        RenderTarget savedRenderTarget = RenderTarget.Targets.ElementAtOrDefault(settings.RenderTargetIndex + 6);
        selectedRenderTarget = RenderTargets.FirstOrDefault(target => target.Name == savedRenderTarget?.Name) ??
            RenderTargets.FirstOrDefault(target => target.Name == "5.1.2 side") ?? RenderTargets[0];

        RenderingSettings.SpeakerVirtualizer = settings.SpeakerVirtualizer;
        RenderingSettings.Force24Bit = settings.Force24Bit;
        RenderingSettings.MuteBed = settings.MuteBed;
        RenderingSettings.MuteGround = settings.MuteGround;
        SurroundSwap = settings.SurroundSwap;
        WavChannelSkip = settings.WavChannelSkip;
        DetailedGrading = settings.DetailedGrading;
        UpmixingSettings.MatrixUpmixing = settings.MatrixUpmixing;
        UpmixingSettings.Cavernize = settings.CavernizeUpmixing;
        UpmixingSettings.Effect = settings.UpmixingEffect ?? .75f;
        UpmixingSettings.Smoothness = settings.UpmixingSmoothness ?? .8f;
        environment = new(this);
        report = new PostRenderReport(environment.Listener, language.RenderReportStrings);
        reportText = report.Report;

        TryLoadSavedHrir();
        TryLoadSavedRoomCorrection();
        QueueJobs.CollectionChanged += (_, _) => {
            OnPropertyChanged(nameof(CanRunQueue));
            OnPropertyChanged(nameof(HasQueueJobs));
        };

        InitializeComponent();
        DataContext = this;
        UpdateRenderTargetDetails();
    }

    protected override void OnOpened(EventArgs e) {
        base.OnOpened(e);
        ApplyViewScale();
        BuildNativeMenu();
        BuildWindowsMenu();
        if (settings.CheckUpdates && !Program.ConsoleMode) {
            _ = CheckForUpdates();
        }
    }

    protected override void OnClosed(EventArgs e) {
        Dispose();
        CheckBlocks();
        base.OnClosed(e);
    }

    public bool InitializeCommandLine(string[] args) {
        bool initialized = CommandLineProcessor.Initialize(args, this);
        SaveSettings();
        return initialized;
    }

    public async Task OpenFile(string path) {
        if (IsBusy) {
            return;
        }

        IsBusy = true;
        try {
            await Task.Run(() => OpenContent(path));
            Status = Text("OpSrcS");
            Warning = NoWarningsText;
            Progress = 0;
            IsProgressIndeterminate = false;
            ReportText = report.Report;
            NotifyProperties(nameof(SuggestedOutputName));
            OnPropertyChanged(nameof(LastDirectory));
        } catch (Exception ex) {
            Status = Text("Error");
            Warning = ex.Message;
        } finally {
            IsBusy = false;
        }
    }

    public async Task RenderTo(string path) {
        if (IsBusy || SelectedTrack == null) {
            return;
        }

        IsBusy = true;
        Warning = NoWarningsText;
        Status = Text("Start");
        cancellation = new CancellationTokenSource();
        bool outputExisted = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        try {
            await Task.Run(() => {
                RenderContent(path);
            }, cancellation.Token);
            ReportText = report.Report;
        } catch (OperationCanceledException) {
            Status = Text("Canc");
            Warning = NoWarningsText;
            DeleteCreatedOutput(path, outputExisted);
        } catch (Exception ex) {
            Status = Text("Error");
            Warning = ex.Message;
            DeleteCreatedOutput(path, outputExisted);
        } finally {
            cancellation.Dispose();
            cancellation = null;
            IsProgressIndeterminate = false;
            IsBusy = false;
        }
    }

    public void AddCurrentToQueue() {
        if (!CanAddToQueue) {
            return;
        }

        QueueJobs.Add(CreateJob(LoadedPath, Tracks.IndexOf(SelectedTrack)));
        Status = Text("QuAdd");
    }

    public void AddFilesToQueue(IEnumerable<string> paths) => AddFilesToQueue(paths, null);

    public void AddFilesToQueue(IEnumerable<string> paths, string outputFolder) {
        if (ReportMode) {
            Status = Text("ReMoH");
            return;
        }

        int added = 0;
        foreach (string path in paths.Where(File.Exists)) {
            QueueJobs.Add(CreateJob(path, null, outputFolder));
            added++;
        }

        if (added != 0) {
            Status = Text("QuAdd");
        }
    }

    public void RemoveSelectedQueueJob() {
        if (!CanRemoveQueueJob) {
            return;
        }

        QueueJobs.Remove(SelectedQueueJob);
        SelectedQueueJob = null;
    }

    public async Task LoadHrir(string path) {
        if (IsBusy || string.IsNullOrWhiteSpace(path)) {
            return;
        }

        IsBusy = true;
        try {
            await Task.Run(() => LoadHrirFile(path));
            settings.HrirPath = path;
            settings.LastDirectory = Path.GetDirectoryName(path);
            HasHrir = true;
            Status = Text("LoadH");
            Warning = NoWarningsText;
            SaveSettings();
            OnPropertyChanged(nameof(LastDirectory));
        } catch (Exception ex) {
            Status = Text("Error");
            Warning = string.Format(Text("IrErr"), ex.Message);
        } finally {
            IsBusy = false;
        }
    }

    public void ResetHrir() {
        if (IsBusy) {
            return;
        }

        VirtualizerFilter.Reset();
        settings.HrirPath = null;
        HasHrir = false;
        Status = Text("LoadV");
        Warning = NoWarningsText;
        SaveSettings();
    }

    public void LoadRoomCorrection(string path) {
        if (IsBusy || string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            this.LoadRoomCorrection(path, language.ConversionStrings);
            roomCorrectionPath = path;
            settings.RoomCorrectionPath = path;
            settings.LastFilterDirectory = Path.GetDirectoryName(path);
            Status = Text("FiltH");
            Warning = NoWarningsText;
            SaveSettings();
            NotifyRoomCorrectionChanged();
            OnPropertyChanged(nameof(LastFilterDirectory));
        } catch (Exception ex) {
            ClearRoomCorrectionState();
            Status = Text("Error");
            Warning = ex.Message;
            SaveSettings();
            NotifyRoomCorrectionChanged();
        }
    }

    public void ClearRoomCorrection() {
        if (IsBusy) {
            return;
        }

        ClearRoomCorrectionState();
        Status = Text("FiltH");
        Warning = NoWarningsText;
        SaveSettings();
        NotifyRoomCorrectionChanged();
    }

    public void SetFfmpegLocation(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        settings.FFmpegPath = path;
        ffmpeg.Location = path;
        SaveSettings();
    }

    public void MarkUpdateChecked() {
        settings.LastUpdateCheck = DateTime.Now;
        SaveSettings();
        OnPropertyChanged(nameof(LastUpdateCheck));
    }

    public string Text(string key) => language.Text(key);

    public string MenuText(string key) => language.MenuText(key);

    public string RenderTargetSelectorText(string key) => language.RenderTargetSelectorText(key);

    public string FileTypeName(string key) {
        string value = Text(key);
        int separator = value.IndexOf('|');
        return separator < 0 ? value : value[..separator];
    }

    public bool SetLanguage(string code) {
        if (IsBusy) {
            Status = Text("OpRun");
            return false;
        }
        if (LanguageCode == code) {
            return false;
        }

        settings.LanguageCode = code;
        settings.Save();
        OnPropertyChanged(nameof(LanguageCode));
        Status = Text("OpRes");
        return true;
    }

    public void ApplyUpmixingSettings(bool matrixUpmixing, bool cavernizeUpmixing, float effect, float smoothness) {
        UpmixingSettings.MatrixUpmixing = matrixUpmixing;
        UpmixingSettings.Cavernize = cavernizeUpmixing;
        UpmixingSettings.Effect = Math.Clamp(effect, 0, 1);
        UpmixingSettings.Smoothness = Math.Clamp(smoothness, 0, 1);
        SaveSettings();
        NotifyProperties(nameof(MatrixUpmixing), nameof(CavernizeUpmixing), nameof(UpmixingEffect),
            nameof(UpmixingSmoothness));
    }

    public async Task RunQueue() {
        if (!CanRunQueue) {
            return;
        }

        IsBusy = true;
        Warning = NoWarningsText;
        Status = Text("QuSta");
        cancellation = new CancellationTokenSource();
        try {
            foreach (QueuedRenderJob job in QueueJobs.ToArray()) {
                await ProcessQueueJob(job, cancellation.Token);
            }

            Status = Text("ExpOk");
        } catch (OperationCanceledException) {
            SetActiveJobStatus(Text("Canc"));
            Status = Text("Canc");
            Warning = NoWarningsText;
        } catch (Exception ex) {
            SetActiveJobStatus(Text("Error"));
            Status = Text("Error");
            Warning = ex.Message;
        } finally {
            activeJob = null;
            cancellation.Dispose();
            cancellation = null;
            IsProgressIndeterminate = false;
            IsBusy = false;
        }
    }

    public void Cancel() => cancellation?.Cancel();

    public string GetWiringText() {
        if (SelectedRenderTarget == null) {
            return Text("LdSrc");
        }

        StringBuilder result = new();
        result.AppendLine(SelectedRenderTarget.Name);
        result.AppendLine();
        ReferenceChannel[] channels = SelectedRenderTarget.GetNameMappedChannels();
        for (int i = 0; i < channels.Length; i++) {
            result.AppendLine($"{i + 1}: {channels[i]}");
        }

        if (SelectedRenderTarget is DownmixedRenderTarget downmixed && downmixed.IsMatrixWired) {
            result.AppendLine();
            foreach ((ReferenceChannel source, ReferenceChannel posPhase, ReferenceChannel negPhase) in downmixed.MatrixWirings) {
                result.AppendLine($"{source}: +{posPhase}, -{negPhase}");
            }
        }

        return result.ToString();
    }

    public string GetPostRenderReportText() => string.IsNullOrWhiteSpace(ReportText) ? report.Report : ReportText;

    public void Dispose() {
        cancellation?.Cancel();
        cancellation?.Dispose();
        SaveSettings();
        LoadedFile?.Dispose();
    }

    internal void UpdateStatus(string text) {
        if (Program.ConsoleMode) {
            Console.WriteLine(text);
            return;
        }
        Dispatcher.UIThread.Post(() => Status = text);
    }

    internal void UpdateProgress(double progressValue) {
        if (Program.ConsoleMode) {
            return;
        }
        Dispatcher.UIThread.Post(() => {
            IsProgressIndeterminate = progressValue < 0;
            if (progressValue >= 0) {
                Progress = Math.Clamp(progressValue, 0, 1);
                if (activeJob != null) {
                    activeJob.Progress = Progress;
                }
            }
        });
    }

    internal void WarningRaised(string text) {
        if (Program.ConsoleMode) {
            Console.Error.WriteLine(text);
            return;
        }
        Dispatcher.UIThread.Post(() => Warning = text);
    }

    void Error(string message) => WarningRaised(message);

    async Task ProcessQueueJob(QueuedRenderJob job, CancellationToken renderCancellationToken) {
        renderCancellationToken.ThrowIfCancellationRequested();
        activeJob = job;
        await OpenQueueJob(job, renderCancellationToken);
        ApplyJobSettings(job);
        SelectQueueTrack(job);
        await RenderQueueJob(job, renderCancellationToken);
        CompleteQueueJob(job);
    }

    async Task OpenQueueJob(QueuedRenderJob job, CancellationToken renderCancellationToken) {
        job.Status = Text("OpSrc");
        job.Progress = 0;
        await Task.Run(() => OpenContent(job.SourcePath), renderCancellationToken);
        ApplyLoadedFile(job.SourcePath);
    }

    void SelectQueueTrack(QueuedRenderJob job) {
        if (job.TrackIndex.HasValue && job.TrackIndex.Value >= 0 && job.TrackIndex.Value < LoadedFile.Tracks.Count) {
            SelectedTrack = LoadedFile.Tracks[job.TrackIndex.Value];
        }
    }

    async Task RenderQueueJob(QueuedRenderJob job, CancellationToken renderCancellationToken) {
        job.Status = Text("Start");
        Status = Text("Start");
        if (OutputPathConflicts(job)) {
            job.OutputPath = CreateOutputPath(job.SourcePath, null);
        }

        bool outputExisted = File.Exists(job.OutputPath);
        try {
            await Task.Run(() => RenderContent(job.OutputPath), renderCancellationToken);
        } catch {
            DeleteCreatedOutput(job.OutputPath, outputExisted);
            throw;
        }
    }

    void CompleteQueueJob(QueuedRenderJob job) {
        job.Progress = 1;
        job.Status = Text("ExpOk");
        ReportText = report.Report;
        QueueJobs.Remove(job);
    }

    void SetActiveJobStatus(string statusText) {
        if (activeJob != null) {
            activeJob.Status = statusText;
        }
    }

    bool OutputPathConflicts(QueuedRenderJob job) =>
        File.Exists(job.OutputPath) ||
        QueueJobs.Any(other => !ReferenceEquals(other, job) &&
            string.Equals(other.OutputPath, job.OutputPath, StringComparison.OrdinalIgnoreCase));

    void ApplyLoadedFile(string path) {
        Tracks.Clear();
        foreach (CavernizeTrack track in LoadedFile.Tracks) {
            Tracks.Add(track);
        }
        LoadedPath = path;
        LoadedTitle = Path.GetFileName(path);
        SelectedTrack = LoadedFile.Tracks
            .Where(track => track.Codec != Codec.Unknown)
            .OrderBy(track => track.Codec)
            .FirstOrDefault() ?? LoadedFile.Tracks.FirstOrDefault();
    }

    void ClearRoomCorrectionState() {
        RenderingSettings.RoomCorrection = null;
        roomCorrectionPath = null;
        settings.RoomCorrectionPath = null;
    }

    void NotifyRoomCorrectionChanged() =>
        NotifyProperties(nameof(HasRoomCorrection), nameof(RoomCorrectionStatus));

    bool SetPersistedOption(bool currentValue, bool newValue, Action<bool> setter,
        [CallerMemberName] string propertyName = null) {
        if (currentValue == newValue) {
            return false;
        }

        setter(newValue);
        SaveSettings();
        OnPropertyChanged(propertyName);
        UpdateMenuState();
        return true;
    }

    void NotifyProperties(params string[] propertyNames) {
        foreach (string propertyName in propertyNames) {
            OnPropertyChanged(propertyName);
        }
    }

    void UpdateCommandState() =>
        NotifyProperties(nameof(CanUseCommands), nameof(CanRender), nameof(CanAddToQueue), nameof(CanRunQueue),
            nameof(CanRemoveQueueJob), nameof(CanCancel));

    static void DeleteCreatedOutput(string path, bool outputExisted) {
        if (!outputExisted && !string.IsNullOrWhiteSpace(path) && File.Exists(path)) {
            File.Delete(path);
        }
    }

    QueuedRenderJob CreateJob(string sourcePath, int? trackIndex) => CreateJob(sourcePath, trackIndex, null);

    QueuedRenderJob CreateJob(string sourcePath, int? trackIndex, string outputFolder) => new(sourcePath,
        CreateOutputPath(sourcePath, outputFolder), trackIndex,
        SelectedExportFormat, SelectedRenderTarget, SpeakerVirtualizer, Force24Bit, MuteBed, MuteGround, SurroundSwap,
        WavChannelSkip, DetailedGrading, MatrixUpmixing, CavernizeUpmixing, UpmixingEffect, UpmixingSmoothness);

    string CreateOutputPath(string sourcePath, string outputFolder) {
        HashSet<string> queuedOutputPaths = QueueJobs
            .Select(job => job.OutputPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string folder = !string.IsNullOrWhiteSpace(outputFolder) ? outputFolder :
            Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory(),
            name = Path.GetFileNameWithoutExtension(sourcePath),
            extension = SuggestedOutputExtension;
        string candidate = Path.Combine(folder, $"{name}.cavernize.{extension}");
        int index = 2;
        while (File.Exists(candidate) || queuedOutputPaths.Contains(candidate)) {
            candidate = Path.Combine(folder, $"{name}.cavernize-{index}.{extension}");
            index++;
        }
        return candidate;
    }

    void ApplyJobSettings(QueuedRenderJob job) {
        SelectedExportFormat = job.ExportFormat;
        SelectedRenderTarget = job.RenderTarget;
        RenderingSettings.SpeakerVirtualizer = job.SpeakerVirtualizer;
        RenderingSettings.Force24Bit = job.Force24Bit;
        RenderingSettings.MuteBed = job.MuteBed;
        RenderingSettings.MuteGround = job.MuteGround;
        SurroundSwap = job.SurroundSwap;
        WavChannelSkip = job.WavChannelSkip;
        DetailedGrading = job.DetailedGrading;
        UpmixingSettings.MatrixUpmixing = job.MatrixUpmixing;
        UpmixingSettings.Cavernize = job.CavernizeUpmixing;
        UpmixingSettings.Effect = job.UpmixingEffect;
        UpmixingSettings.Smoothness = job.UpmixingSmoothness;
        ReportMode = false;
    }

    void SaveSettings() {
        settings.OutputCodecIndex = Array.IndexOf(ExportFormats, SelectedExportFormat) - 2;
        settings.RenderTargetIndex = Array.FindIndex(RenderTarget.Targets,
            target => target.Name == SelectedRenderTarget?.Name) - 6;
        settings.SpeakerVirtualizer = RenderingSettings.SpeakerVirtualizer;
        settings.Force24Bit = RenderingSettings.Force24Bit;
        settings.MuteBed = RenderingSettings.MuteBed;
        settings.MuteGround = RenderingSettings.MuteGround;
        settings.SurroundSwap = SurroundSwap;
        settings.WavChannelSkip = WavChannelSkip;
        settings.DetailedGrading = DetailedGrading;
        settings.MatrixUpmixing = UpmixingSettings.MatrixUpmixing;
        settings.CavernizeUpmixing = UpmixingSettings.Cavernize;
        settings.UpmixingEffect = UpmixingSettings.Effect;
        settings.UpmixingSmoothness = UpmixingSettings.Smoothness;
        settings.RoomCorrectionPath = roomCorrectionPath;
        settings.Save();
    }

    void TryLoadSavedHrir() {
        if (string.IsNullOrWhiteSpace(settings.HrirPath) || !File.Exists(settings.HrirPath)) {
            settings.HrirPath = null;
            return;
        }

        try {
            LoadHrirFile(settings.HrirPath);
            hasHrir = true;
        } catch (Exception ex) {
            settings.HrirPath = null;
            Warning = string.Format(Text("IrErr"), ex.Message);
        }
    }

    void TryLoadSavedRoomCorrection() {
        if (string.IsNullOrWhiteSpace(settings.RoomCorrectionPath) || !File.Exists(settings.RoomCorrectionPath)) {
            settings.RoomCorrectionPath = null;
            return;
        }

        try {
            this.LoadRoomCorrection(settings.RoomCorrectionPath, language.ConversionStrings);
            roomCorrectionPath = settings.RoomCorrectionPath;
        } catch (Exception ex) {
            settings.RoomCorrectionPath = null;
            Warning = ex.Message;
        }
    }

    void UpdateTrackDetails(CavernizeTrack track) {
        TrackFormatHeader = track?.FormatHeader;
        (string property, string value)[] details = track?.Details ?? [];
        TrackDetail1Title = details.Length > 0 ? details[0].property : null;
        TrackDetail1Value = details.Length > 0 ? details[0].value : null;
        TrackDetail2Title = details.Length > 1 ? details[1].property : null;
        TrackDetail2Value = details.Length > 1 ? details[1].value : null;
        TrackDetail3Title = details.Length > 2 ? details[2].property : null;
        TrackDetail3Value = details.Length > 2 ? details[2].value : null;
    }

    void UpdateRenderTargetDetails() {
        if (SelectedRenderTarget == null) {
            ActiveChannels = null;
            return;
        }

        ReferenceChannel[] channels = SelectedRenderTarget.GetNameMappedChannels();
        ActiveChannels = string.Join(", ", channels.Select((channel, index) =>
            SelectedRenderTarget.IsExported(index) ? channel.ToString() : $"{channel} (mixed)"));
    }

    void LoadHrirFile(string path) {
        using RIFFWaveReader file = new(path);
        file.ReadHeader();
        VirtualizerFilter.Override(
            VirtualChannel.Parse(new MultichannelWaveform(file.ReadMultichannelAfterHeader()), file.SampleRate),
            file.SampleRate);
    }

    void ThrowIfCancellationRequested() => cancellation?.Token.ThrowIfCancellationRequested();

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    sealed class CallbackFFmpeg : FFmpeg {
        readonly Action<string> statusChanged;

        public CallbackFFmpeg(Action<string> statusChanged) : this(statusChanged, null) { }

        public CallbackFFmpeg(Action<string> statusChanged, string lastLocation) {
            this.statusChanged = statusChanged;
            Location = lastLocation;
        }

        public override void UpdateStatusText(string text) => statusChanged?.Invoke(text);
    }

    sealed class AppSettings {
        readonly CavernizeGUI.Resources.Settings settings = CavernizeGUI.Resources.Settings.Default;
        readonly CavernizeGUI.Resources.UpmixingSettings upmixingSettings =
            CavernizeGUI.Resources.UpmixingSettings.Default;

        double viewScale = 1;
        bool muteBed;
        bool muteGround;
        bool detailedGrading;
        string roomCorrectionPath;

        AppSettings() { }

        public string LastDirectory {
            get => settings.lastDirectory;
            set => settings.lastDirectory = value;
        }

        public string LastFilterDirectory {
            get => settings.lastOutputFilters;
            set => settings.lastOutputFilters = value;
        }

        public string HrirPath {
            get => settings.hrirPath;
            set => settings.hrirPath = value;
        }

        public string RoomCorrectionPath {
            get => roomCorrectionPath;
            set => roomCorrectionPath = value;
        }

        public string FFmpegPath {
            get => settings.ffmpegLocation;
            set => settings.ffmpegLocation = value;
        }

        public string LanguageCode {
            get => settings.language;
            set => settings.language = value;
        }

        public DateTime LastUpdateCheck {
            get => settings.lastUpdate;
            set => settings.lastUpdate = value;
        }

        public double ViewScale {
            get => viewScale;
            set => viewScale = value;
        }

        public int OutputCodecIndex {
            get => settings.outputCodec;
            set => settings.outputCodec = value;
        }

        public int RenderTargetIndex {
            get => settings.renderTarget;
            set => settings.renderTarget = value;
        }

        public bool SpeakerVirtualizer {
            get => settings.speakerVirtualizer;
            set => settings.speakerVirtualizer = value;
        }

        public bool Force24Bit {
            get => settings.force24Bit;
            set => settings.force24Bit = value;
        }

        public bool MuteBed {
            get => muteBed;
            set => muteBed = value;
        }

        public bool MuteGround {
            get => muteGround;
            set => muteGround = value;
        }

        public bool SurroundSwap {
            get => settings.surroundSwap;
            set => settings.surroundSwap = value;
        }

        public bool WavChannelSkip {
            get => settings.wavChannelSkip;
            set => settings.wavChannelSkip = value;
        }

        public bool DetailedGrading {
            get => detailedGrading;
            set => detailedGrading = value;
        }

        public bool CheckUpdates {
            get => settings.checkUpdates;
            set => settings.checkUpdates = value;
        }

        public bool MatrixUpmixing {
            get => upmixingSettings.MatrixUpmix;
            set => upmixingSettings.MatrixUpmix = value;
        }

        public bool CavernizeUpmixing {
            get => upmixingSettings.Cavernize;
            set => upmixingSettings.Cavernize = value;
        }

        public float? UpmixingEffect {
            get => upmixingSettings.Effect;
            set => upmixingSettings.Effect = value ?? .75f;
        }

        public float? UpmixingSmoothness {
            get => upmixingSettings.Smoothness;
            set => upmixingSettings.Smoothness = value ?? .8f;
        }

        public static AppSettings Load() => new();

        public void Save() {
            settings.Save();
            upmixingSettings.Save();
        }
    }
}
