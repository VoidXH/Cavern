using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

using Avalonia.Threading;
using Cavern.Channels;
using Cavern.Format.Common;
using Cavernize.Logic.CommandLine;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using CavernizeGUI.CavernSettings;
using VoidX.WPF.FFmpeg;

using GuiLanguage = CavernizeGUI.Consts.Language;

namespace CavernizeGUI;

public sealed class MainViewModel : ObservableObject, IDisposable {
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
                session.SelectedTrack = value;
                UpdateTrackDetails(value);
                OnPropertyChanged(nameof(CanRender));
            }
        }
    }

    public ExportFormat SelectedExportFormat {
        get => selectedExportFormat;
        set {
            if (SetProperty(ref selectedExportFormat, value)) {
                session.ExportFormat = value;
                SaveSettings();
                NotifyProperties(nameof(SuggestedOutputName), nameof(SuggestedOutputExtension));
            }
        }
    }

    public RenderTarget SelectedRenderTarget {
        get => selectedRenderTarget;
        set {
            if (SetProperty(ref selectedRenderTarget, value)) {
                session.RenderTarget = value;
                UpdateRenderTargetDetails();
                if (session.RenderingSettings.RoomCorrection != null) {
                    ClearRoomCorrectionState();
                    Status = "Room correction cleared because the layout changed.";
                    NotifyRoomCorrectionChanged();
                }
                SaveSettings();
            }
        }
    }

    public bool SpeakerVirtualizer {
        get => session.RenderingSettings.SpeakerVirtualizer;
        set => SetPersistedOption(session.RenderingSettings.SpeakerVirtualizer, value,
            newValue => session.RenderingSettings.SpeakerVirtualizer = newValue);
    }

    public bool Force24Bit {
        get => session.RenderingSettings.Force24Bit;
        set => SetPersistedOption(session.RenderingSettings.Force24Bit, value,
            newValue => session.RenderingSettings.Force24Bit = newValue);
    }

    public bool MuteBed {
        get => session.RenderingSettings.MuteBed;
        set => SetPersistedOption(session.RenderingSettings.MuteBed, value,
            newValue => session.RenderingSettings.MuteBed = newValue);
    }

    public bool MuteGround {
        get => session.RenderingSettings.MuteGround;
        set => SetPersistedOption(session.RenderingSettings.MuteGround, value,
            newValue => session.RenderingSettings.MuteGround = newValue);
    }

    public bool SurroundSwap {
        get => session.SurroundSwap;
        set => SetPersistedOption(session.SurroundSwap, value, newValue => session.SurroundSwap = newValue);
    }

    public bool WavChannelSkip {
        get => session.WavChannelSkip;
        set => SetPersistedOption(session.WavChannelSkip, value, newValue => session.WavChannelSkip = newValue);
    }

    public bool DetailedGrading {
        get => session.DetailedGrading;
        set => SetPersistedOption(session.DetailedGrading, value, newValue => session.DetailedGrading = newValue);
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
        get => session.ReportMode;
        set {
            if (session.ReportMode != value) {
                session.ReportMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddToQueue));
            }
        }
    }

    public bool MatrixUpmixing => session.UpmixingSettings.MatrixUpmixing;

    public bool CavernizeUpmixing => session.UpmixingSettings.Cavernize;

    public float UpmixingEffect => session.UpmixingSettings.Effect;

    public float UpmixingSmoothness => session.UpmixingSettings.Smoothness;

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
                OnPropertyChanged(nameof(CanUseCommands));
                OnPropertyChanged(nameof(CanRender));
                OnPropertyChanged(nameof(CanAddToQueue));
                OnPropertyChanged(nameof(CanRunQueue));
                OnPropertyChanged(nameof(CanRemoveQueueJob));
                OnPropertyChanged(nameof(CanCancel));
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

    public bool HasRoomCorrection => session.RenderingSettings.RoomCorrection != null;

    public string HrirStatus => HasHrir ? $"HRIR: {Path.GetFileName(settings.HrirPath)}" : "HRIR: built-in";

    public string RoomCorrectionStatus => HasRoomCorrection ? $"Filters: {Path.GetFileName(roomCorrectionPath)}" : "Filters: none";

    public DateTime LastUpdateCheck => settings.LastUpdateCheck;

    public string SuggestedOutputExtension {
        get {
            string filter = SelectedExportFormat?.Codec.GetSaveDialogFilter();
            int marker = filter?.IndexOf("*.") ?? -1;
            if (marker >= 0) {
                int end = filter.IndexOfAny([';', '|'], marker);
                return filter[(marker + 2)..(end < 0 ? filter.Length : end)];
            }
            return SelectedExportFormat?.Codec == Codec.PCM_LE || SelectedExportFormat?.Codec == Codec.PCM_Float ? "wav" : "mkv";
        }
    }

    public string SuggestedOutputName {
        get {
            string stem = string.IsNullOrWhiteSpace(LoadedPath) ? "Cavernize Render" : Path.GetFileNameWithoutExtension(LoadedPath);
            return $"{stem}.{SuggestedOutputExtension}";
        }
    }

    readonly AppSettings settings = AppSettings.Load();
    readonly GuiLanguage language;
    readonly CavernizeSession session;
    CancellationTokenSource cancellation;
    QueuedRenderJob activeJob;
    QueuedRenderJob selectedQueueJob;
    string roomCorrectionPath;
    string loadedPath;
    string loadedTitle;
    CavernizeTrack selectedTrack;
    ExportFormat selectedExportFormat;
    RenderTarget selectedRenderTarget;
    double progress;
    bool isProgressIndeterminate;
    bool isBusy;
    bool hasHrir;
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

    internal CavernizeSession Session => session;

    public MainViewModel() {
        language = GuiLanguage.Create(settings.LanguageCode);
        FFmpeg.ReadyText = Text("FFRea");
        FFmpeg.NotReadyText = Text("FFNRe");
        session = new(language, new DynamicUpmixingSettings(), new DynamicSpecialRenderModeSettings());
        loadedTitle = Text("NoSrc");
        status = Text("OpSrcS");
        warning = NoWarningsText;
        reportText = session.Report.Report;

        ExportFormats = ExportFormat.GetFormats(language.TrackStrings);
        RenderTargets = RenderTarget.Targets.Where(target => target is not DriverRenderTarget).ToArray();
        selectedExportFormat = ExportFormats.ElementAtOrDefault(settings.OutputCodecIndex + 2) ??
            ExportFormats.First(format => format.Codec == Codec.PCM_LE);
        RenderTarget savedRenderTarget = RenderTarget.Targets.ElementAtOrDefault(settings.RenderTargetIndex + 6);
        selectedRenderTarget = RenderTargets.FirstOrDefault(target => target.Name == savedRenderTarget?.Name) ??
            RenderTargets.FirstOrDefault(target => target.Name == "5.1.2 side") ?? RenderTargets[0];
        session.ExportFormat = selectedExportFormat;
        session.RenderTarget = selectedRenderTarget;
        UpdateRenderTargetDetails();
        session.RenderingSettings.SpeakerVirtualizer = settings.SpeakerVirtualizer;
        session.RenderingSettings.Force24Bit = settings.Force24Bit;
        session.RenderingSettings.MuteBed = settings.MuteBed;
        session.RenderingSettings.MuteGround = settings.MuteGround;
        session.SurroundSwap = settings.SurroundSwap;
        session.WavChannelSkip = settings.WavChannelSkip;
        session.DetailedGrading = settings.DetailedGrading;
        session.UpmixingSettings.MatrixUpmixing = settings.MatrixUpmixing;
        session.UpmixingSettings.Cavernize = settings.CavernizeUpmixing;
        session.UpmixingSettings.Effect = settings.UpmixingEffect ?? .75f;
        session.UpmixingSettings.Smoothness = settings.UpmixingSmoothness ?? .8f;
        if (!string.IsNullOrWhiteSpace(settings.FFmpegPath)) {
            session.FFmpeg.Location = settings.FFmpegPath;
        }
        TryLoadSavedHrir();
        TryLoadSavedRoomCorrection();
        QueueJobs.CollectionChanged += (_, _) => {
            OnPropertyChanged(nameof(CanRunQueue));
            OnPropertyChanged(nameof(HasQueueJobs));
        };

        session.ProgressChanged += progressValue => Dispatcher.UIThread.Post(() => {
            IsProgressIndeterminate = progressValue < 0;
            if (progressValue >= 0) {
                Progress = Math.Clamp(progressValue, 0, 1);
                if (activeJob != null) {
                    activeJob.Progress = Progress;
                }
            }
        });
        session.StatusChanged += text => Dispatcher.UIThread.Post(() => Status = text);
        session.WarningRaised += text => Dispatcher.UIThread.Post(() => Warning = text);
    }

    public bool InitializeCommandLine(string[] args) {
        bool initialized = CommandLineProcessor.Initialize(args, session);
        if (!initialized) {
            return false;
        }

        SyncFromSession();
        SaveSettings();
        return true;
    }

    public async Task OpenFile(string path) {
        if (IsBusy) {
            return;
        }

        IsBusy = true;
        try {
            await Task.Run(() => session.OpenContent(path));
            ApplyLoadedFile(path);
            settings.LastDirectory = Path.GetDirectoryName(path);
            SelectedTrack = session.SelectedTrack;
            Status = $"Opened {LoadedTitle}.";
            Warning = NoWarningsText;
            Progress = 0;
            IsProgressIndeterminate = false;
            ReportText = session.Report.Report;
            SaveSettings();
            NotifyProperties(nameof(SuggestedOutputName));
            OnPropertyChanged(nameof(LastDirectory));
        } catch (Exception ex) {
            Status = "Open failed.";
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
            await session.RenderAsync(path, cancellation.Token);
            ReportText = session.Report.Report;
        } catch (OperationCanceledException) {
            Status = "Render canceled.";
            Warning = NoWarningsText;
            DeleteCreatedOutput(path, outputExisted);
        } catch (Exception ex) {
            Status = "Render failed.";
            Warning = ex.Message;
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
        Status = $"Queued {LoadedTitle}.";
    }

    public void AddFilesToQueue(IEnumerable<string> paths) => AddFilesToQueue(paths, null);

    public void AddFilesToQueue(IEnumerable<string> paths, string outputFolder) {
        if (ReportMode) {
            Status = "Disable report only before queueing files.";
            return;
        }

        int added = 0;
        foreach (string path in paths.Where(File.Exists)) {
            QueueJobs.Add(CreateJob(path, null, outputFolder));
            added++;
        }

        if (added != 0) {
            Status = $"Queued {added} file{(added == 1 ? string.Empty : "s")}.";
        }
    }

    public void RemoveSelectedQueueJob() {
        if (!CanRemoveQueueJob) {
            return;
        }

        QueueJobs.Remove(SelectedQueueJob);
        SelectedQueueJob = null;
        Status = "Removed queued job.";
    }

    public async Task LoadHrir(string path) {
        if (IsBusy || string.IsNullOrWhiteSpace(path)) {
            return;
        }

        IsBusy = true;
        try {
            await Task.Run(() => session.LoadHrir(path));
            settings.HrirPath = path;
            settings.LastDirectory = Path.GetDirectoryName(path);
            HasHrir = true;
            Status = $"Loaded HRIR {Path.GetFileName(path)}.";
            Warning = NoWarningsText;
            SaveSettings();
            OnPropertyChanged(nameof(LastDirectory));
        } catch (Exception ex) {
            Status = "HRIR load failed.";
            Warning = $"The impulse response file is invalid. Error: {ex.Message}";
        } finally {
            IsBusy = false;
        }
    }

    public void ResetHrir() {
        if (IsBusy) {
            return;
        }

        session.ResetHrir();
        settings.HrirPath = null;
        HasHrir = false;
        Status = "Restored built-in HRIR.";
        Warning = NoWarningsText;
        SaveSettings();
    }

    public void LoadRoomCorrection(string path) {
        if (IsBusy || string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            session.LoadRoomCorrection(path, language.ConversionStrings);
            roomCorrectionPath = path;
            settings.RoomCorrectionPath = path;
            settings.LastFilterDirectory = Path.GetDirectoryName(path);
            Status = $"Loaded filters {Path.GetFileName(path)}.";
            Warning = NoWarningsText;
            SaveSettings();
            NotifyRoomCorrectionChanged();
            OnPropertyChanged(nameof(LastFilterDirectory));
        } catch (Exception ex) {
            ClearRoomCorrectionState();
            Status = "Filter load failed.";
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
        Status = "Cleared room correction filters.";
        Warning = NoWarningsText;
        SaveSettings();
        NotifyRoomCorrectionChanged();
    }

    public void SetFfmpegLocation(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        settings.FFmpegPath = path;
        session.FFmpeg.Location = path;
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

    public string FileTypeName(string key) => language.FileTypeName(key);

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
        session.UpmixingSettings.MatrixUpmixing = matrixUpmixing;
        session.UpmixingSettings.Cavernize = cavernizeUpmixing;
        session.UpmixingSettings.Effect = Math.Clamp(effect, 0, 1);
        session.UpmixingSettings.Smoothness = Math.Clamp(smoothness, 0, 1);
        SaveSettings();
        NotifyProperties(nameof(MatrixUpmixing), nameof(CavernizeUpmixing), nameof(UpmixingEffect), nameof(UpmixingSmoothness));
    }

    public async Task RunQueue() {
        if (!CanRunQueue) {
            return;
        }

        IsBusy = true;
        Warning = NoWarningsText;
        Status = "Rendering queue...";
        cancellation = new CancellationTokenSource();
        try {
            foreach (QueuedRenderJob job in QueueJobs.ToArray()) {
                await ProcessQueueJob(job, cancellation.Token);
            }

            Status = "Queue completed.";
        } catch (OperationCanceledException) {
            SetActiveJobStatus("Canceled");
            Status = "Queue canceled.";
            Warning = NoWarningsText;
        } catch (Exception ex) {
            SetActiveJobStatus("Failed");
            Status = "Queue failed.";
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
            return "No render target selected.";
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
            result.AppendLine("Matrix wiring:");
            foreach ((ReferenceChannel source, ReferenceChannel posPhase, ReferenceChannel negPhase) in downmixed.MatrixWirings) {
                result.AppendLine($"{source}: +{posPhase}, -{negPhase}");
            }
        }

        return result.ToString();
    }

    public string GetMetadataText() {
        if (SelectedTrack == null) {
            return "Please load a file first.";
        }

        ReadableMetadata metadata = SelectedTrack.GetMetadata();
        if (metadata == null) {
            return "The Cavern API does not yet support displaying the metadata of the selected track.";
        }

        StringBuilder result = new();
        foreach (ReadableMetadataHeader header in metadata.Headers) {
            result.AppendLine($"{header.Name} ({header.Fields.Count})");
            foreach (ReadableMetadataField field in header.Fields) {
                result.AppendLine($"  {field.Name}: {field.Value} - {field.Description}");
            }
            result.AppendLine();
        }
        return result.ToString();
    }

    public string GetPostRenderReportText() => string.IsNullOrWhiteSpace(ReportText) ? session.Report.Report : ReportText;

    public void Dispose() {
        cancellation?.Cancel();
        cancellation?.Dispose();
        SaveSettings();
        session.Dispose();
    }

    async Task ProcessQueueJob(QueuedRenderJob job, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        activeJob = job;
        await OpenQueueJob(job, cancellationToken);
        ApplyJobSettings(job);
        SelectQueueTrack(job);
        await RenderQueueJob(job, cancellationToken);
        CompleteQueueJob(job);
    }

    async Task OpenQueueJob(QueuedRenderJob job, CancellationToken cancellationToken) {
        job.Status = "Opening";
        job.Progress = 0;
        await Task.Run(() => session.OpenContent(job.SourcePath), cancellationToken);
        ApplyLoadedFile(job.SourcePath);
    }

    void SelectQueueTrack(QueuedRenderJob job) {
        if (job.TrackIndex.HasValue && job.TrackIndex.Value >= 0 && job.TrackIndex.Value < session.LoadedFile.Tracks.Count) {
            session.SelectedTrack = session.LoadedFile.Tracks[job.TrackIndex.Value];
        }
        SelectedTrack = session.SelectedTrack;
    }

    async Task RenderQueueJob(QueuedRenderJob job, CancellationToken cancellationToken) {
        job.Status = "Rendering";
        Status = $"Rendering {job.DisplayName}...";
        if (OutputPathConflicts(job)) {
            job.OutputPath = CreateOutputPath(job.SourcePath, null);
        }

        bool outputExisted = File.Exists(job.OutputPath);
        try {
            await session.RenderAsync(job.OutputPath, cancellationToken);
        } catch {
            DeleteCreatedOutput(job.OutputPath, outputExisted);
            throw;
        }
    }

    void CompleteQueueJob(QueuedRenderJob job) {
        job.Progress = 1;
        job.Status = "Done";
        ReportText = session.Report.Report;
        QueueJobs.Remove(job);
    }

    void SetActiveJobStatus(string status) {
        if (activeJob != null) {
            activeJob.Status = status;
        }
    }

    bool OutputPathConflicts(QueuedRenderJob job) =>
        File.Exists(job.OutputPath) ||
        QueueJobs.Any(other => !ReferenceEquals(other, job) &&
            string.Equals(other.OutputPath, job.OutputPath, StringComparison.OrdinalIgnoreCase));

    void ApplyLoadedFile(string path) {
        Tracks.Clear();
        foreach (CavernizeTrack track in session.LoadedFile.Tracks) {
            Tracks.Add(track);
        }
        LoadedPath = path;
        LoadedTitle = Path.GetFileName(path);
    }

    void ClearRoomCorrectionState() {
        session.RenderingSettings.RoomCorrection = null;
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
        return true;
    }

    void NotifyProperties(params string[] propertyNames) {
        foreach (string propertyName in propertyNames) {
            OnPropertyChanged(propertyName);
        }
    }

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

    internal void SyncFromSession() {
        SelectedExportFormat = ExportFormats.FirstOrDefault(format => format.Codec == session.ExportFormat.Codec) ??
            SelectedExportFormat;
        SelectedRenderTarget = RenderTargets.FirstOrDefault(target => target.Name == session.RenderTarget.Name) ??
            SelectedRenderTarget;
        if (session.LoadedFile != null) {
            ApplyLoadedFile(session.LoadedFile.Path);
            SelectedTrack = session.SelectedTrack;
        }
        ReportText = session.Report.Report;
        NotifyProperties(nameof(SuggestedOutputName), nameof(SuggestedOutputExtension));
    }

    void ApplyJobSettings(QueuedRenderJob job) {
        session.ExportFormat = job.ExportFormat;
        session.RenderTarget = job.RenderTarget;
        session.RenderingSettings.SpeakerVirtualizer = job.SpeakerVirtualizer;
        session.RenderingSettings.Force24Bit = job.Force24Bit;
        session.RenderingSettings.MuteBed = job.MuteBed;
        session.RenderingSettings.MuteGround = job.MuteGround;
        session.SurroundSwap = job.SurroundSwap;
        session.WavChannelSkip = job.WavChannelSkip;
        session.DetailedGrading = job.DetailedGrading;
        session.UpmixingSettings.MatrixUpmixing = job.MatrixUpmixing;
        session.UpmixingSettings.Cavernize = job.CavernizeUpmixing;
        session.UpmixingSettings.Effect = job.UpmixingEffect;
        session.UpmixingSettings.Smoothness = job.UpmixingSmoothness;
        session.ReportMode = false;
    }

    void SaveSettings() {
        settings.OutputCodecIndex = Array.IndexOf(ExportFormats, SelectedExportFormat) - 2;
        settings.RenderTargetIndex = Array.FindIndex(RenderTarget.Targets,
            target => target.Name == SelectedRenderTarget?.Name) - 6;
        settings.SpeakerVirtualizer = session.RenderingSettings.SpeakerVirtualizer;
        settings.Force24Bit = session.RenderingSettings.Force24Bit;
        settings.MuteBed = session.RenderingSettings.MuteBed;
        settings.MuteGround = session.RenderingSettings.MuteGround;
        settings.SurroundSwap = session.SurroundSwap;
        settings.WavChannelSkip = session.WavChannelSkip;
        settings.DetailedGrading = session.DetailedGrading;
        settings.MatrixUpmixing = session.UpmixingSettings.MatrixUpmixing;
        settings.CavernizeUpmixing = session.UpmixingSettings.Cavernize;
        settings.UpmixingEffect = session.UpmixingSettings.Effect;
        settings.UpmixingSmoothness = session.UpmixingSettings.Smoothness;
        settings.RoomCorrectionPath = roomCorrectionPath;
        settings.Save();
    }

    void TryLoadSavedHrir() {
        if (string.IsNullOrWhiteSpace(settings.HrirPath) || !File.Exists(settings.HrirPath)) {
            settings.HrirPath = null;
            return;
        }

        try {
            session.LoadHrir(settings.HrirPath);
            hasHrir = true;
        } catch (Exception ex) {
            settings.HrirPath = null;
            Warning = $"Saved HRIR failed to load: {ex.Message}";
        }
    }

    void TryLoadSavedRoomCorrection() {
        if (string.IsNullOrWhiteSpace(settings.RoomCorrectionPath) || !File.Exists(settings.RoomCorrectionPath)) {
            settings.RoomCorrectionPath = null;
            return;
        }

        try {
            session.LoadRoomCorrection(settings.RoomCorrectionPath, language.ConversionStrings);
            roomCorrectionPath = settings.RoomCorrectionPath;
        } catch (Exception ex) {
            settings.RoomCorrectionPath = null;
            Warning = $"Saved filters failed to load: {ex.Message}";
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

    sealed class AppSettings {
        readonly Resources.Settings settings = Resources.Settings.Default;
        readonly Resources.UpmixingSettings upmixingSettings = Resources.UpmixingSettings.Default;

        double viewScale = 1;
        bool muteBed;
        bool muteGround;
        bool detailedGrading;
        string roomCorrectionPath;

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
