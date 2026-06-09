using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

using Avalonia.Threading;
using Cavern.Channels;
using Cavern.Format.Common;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;

namespace CavernizeAvalonia;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable {
    public event PropertyChangedEventHandler PropertyChanged;

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
                OnPropertyChanged(nameof(SuggestedOutputName));
                OnPropertyChanged(nameof(SuggestedOutputExtension));
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
                    session.RenderingSettings.RoomCorrection = null;
                    roomCorrectionPath = null;
                    settings.RoomCorrectionPath = null;
                    Status = "Room correction cleared because the layout changed.";
                    OnPropertyChanged(nameof(HasRoomCorrection));
                    OnPropertyChanged(nameof(RoomCorrectionStatus));
                }
                SaveSettings();
            }
        }
    }

    public bool SpeakerVirtualizer {
        get => session.RenderingSettings.SpeakerVirtualizer;
        set {
            if (session.RenderingSettings.SpeakerVirtualizer != value) {
                session.RenderingSettings.SpeakerVirtualizer = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool Force24Bit {
        get => session.RenderingSettings.Force24Bit;
        set {
            if (session.RenderingSettings.Force24Bit != value) {
                session.RenderingSettings.Force24Bit = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool MuteBed {
        get => session.RenderingSettings.MuteBed;
        set {
            if (session.RenderingSettings.MuteBed != value) {
                session.RenderingSettings.MuteBed = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool MuteGround {
        get => session.RenderingSettings.MuteGround;
        set {
            if (session.RenderingSettings.MuteGround != value) {
                session.RenderingSettings.MuteGround = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool SurroundSwap {
        get => session.SurroundSwap;
        set {
            if (session.SurroundSwap != value) {
                session.SurroundSwap = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool WavChannelSkip {
        get => session.WavChannelSkip;
        set {
            if (session.WavChannelSkip != value) {
                session.WavChannelSkip = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public bool DetailedGrading {
        get => session.DetailedGrading;
        set {
            if (session.DetailedGrading != value) {
                session.DetailedGrading = value;
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

    public string Footer => session.FFmpeg.Found ? "FFmpeg ready." : "FFmpeg not found; FFmpeg-only codecs are unavailable.";

    public bool IsFfmpegMissing => !session.FFmpeg.Found;

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
    readonly CavernizeSession session = new();
    CancellationTokenSource cancellation;
    QueuedRenderJob activeJob;
    QueuedRenderJob selectedQueueJob;
    string roomCorrectionPath;
    string loadedPath;
    string loadedTitle = "No source loaded";
    CavernizeTrack selectedTrack;
    ExportFormat selectedExportFormat;
    RenderTarget selectedRenderTarget;
    double progress;
    bool isProgressIndeterminate;
    bool isBusy;
    bool hasHrir;
    string status = "Open a source file.";
    string warning = "No warnings.";
    string reportText = "No report yet.";
    string trackFormatHeader;
    string trackDetail1Title;
    string trackDetail1Value;
    string trackDetail2Title;
    string trackDetail2Value;
    string trackDetail3Title;
    string trackDetail3Value;
    string activeChannels;

    public MainViewModel() {
        ExportFormats = ExportFormat.GetFormats(new());
        RenderTargets = RenderTarget.Targets.Where(target => target is not DriverRenderTarget).ToArray();
        selectedExportFormat = ExportFormats.FirstOrDefault(format => format.Codec.ToString() == settings.ExportCodec) ??
            ExportFormats.First(format => format.Codec == Codec.PCM_LE);
        selectedRenderTarget = RenderTargets.FirstOrDefault(target => target.Name == settings.RenderTarget) ??
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
            OnPropertyChanged(nameof(Footer));
            OnPropertyChanged(nameof(IsFfmpegMissing));
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

    public async Task OpenFile(string path) {
        if (IsBusy) {
            return;
        }

        IsBusy = true;
        try {
            await Task.Run(() => session.OpenContent(path));
            Tracks.Clear();
            foreach (CavernizeTrack track in session.LoadedFile.Tracks) {
                Tracks.Add(track);
            }
            LoadedPath = path;
            LoadedTitle = Path.GetFileName(path);
            settings.LastDirectory = Path.GetDirectoryName(path);
            SelectedTrack = session.SelectedTrack;
            Status = $"Opened {LoadedTitle}.";
            Warning = "No warnings.";
            Progress = 0;
            IsProgressIndeterminate = false;
            ReportText = session.Report.Report;
            SaveSettings();
            OnPropertyChanged(nameof(SuggestedOutputName));
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
        Warning = "No warnings.";
        Status = "Rendering...";
        cancellation = new CancellationTokenSource();
        bool outputExisted = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        try {
            await session.RenderAsync(path, cancellation.Token);
            ReportText = session.Report.Report;
        } catch (OperationCanceledException) {
            Status = "Render canceled.";
            Warning = "No warnings.";
            if (!outputExisted && !string.IsNullOrWhiteSpace(path)) {
                File.Delete(path);
            }
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
        OnPropertyChanged(nameof(CanRunQueue));
        OnPropertyChanged(nameof(HasQueueJobs));
    }

    public void AddFilesToQueue(IEnumerable<string> paths) {
        if (ReportMode) {
            Status = "Disable report only before queueing files.";
            return;
        }

        int added = 0;
        foreach (string path in paths.Where(File.Exists)) {
            QueueJobs.Add(CreateJob(path, null));
            added++;
        }

        if (added != 0) {
            Status = $"Queued {added} file{(added == 1 ? string.Empty : "s")}.";
            OnPropertyChanged(nameof(CanRunQueue));
            OnPropertyChanged(nameof(HasQueueJobs));
        }
    }

    public void RemoveSelectedQueueJob() {
        if (!CanRemoveQueueJob) {
            return;
        }

        QueueJobs.Remove(SelectedQueueJob);
        SelectedQueueJob = null;
        Status = "Removed queued job.";
        OnPropertyChanged(nameof(CanRunQueue));
        OnPropertyChanged(nameof(HasQueueJobs));
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
            Warning = "No warnings.";
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
        Warning = "No warnings.";
        SaveSettings();
    }

    public void LoadRoomCorrection(string path) {
        if (IsBusy || string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            session.LoadRoomCorrection(path, new ConversionStrings());
            roomCorrectionPath = path;
            settings.RoomCorrectionPath = path;
            settings.LastFilterDirectory = Path.GetDirectoryName(path);
            Status = $"Loaded filters {Path.GetFileName(path)}.";
            Warning = "No warnings.";
            SaveSettings();
            OnPropertyChanged(nameof(HasRoomCorrection));
            OnPropertyChanged(nameof(RoomCorrectionStatus));
            OnPropertyChanged(nameof(LastFilterDirectory));
        } catch (Exception ex) {
            session.RenderingSettings.RoomCorrection = null;
            roomCorrectionPath = null;
            settings.RoomCorrectionPath = null;
            Status = "Filter load failed.";
            Warning = ex.Message;
            SaveSettings();
            OnPropertyChanged(nameof(HasRoomCorrection));
            OnPropertyChanged(nameof(RoomCorrectionStatus));
        }
    }

    public void ClearRoomCorrection() {
        if (IsBusy) {
            return;
        }

        session.RenderingSettings.RoomCorrection = null;
        roomCorrectionPath = null;
        settings.RoomCorrectionPath = null;
        Status = "Cleared room correction filters.";
        Warning = "No warnings.";
        SaveSettings();
        OnPropertyChanged(nameof(HasRoomCorrection));
        OnPropertyChanged(nameof(RoomCorrectionStatus));
    }

    public void SetFfmpegLocation(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        settings.FFmpegPath = path;
        session.FFmpeg.Location = path;
        SaveSettings();
        OnPropertyChanged(nameof(Footer));
        OnPropertyChanged(nameof(IsFfmpegMissing));
    }

    public void ApplyUpmixingSettings(bool matrixUpmixing, bool cavernizeUpmixing, float effect, float smoothness) {
        session.UpmixingSettings.MatrixUpmixing = matrixUpmixing;
        session.UpmixingSettings.Cavernize = cavernizeUpmixing;
        session.UpmixingSettings.Effect = Math.Clamp(effect, 0, 1);
        session.UpmixingSettings.Smoothness = Math.Clamp(smoothness, 0, 1);
        SaveSettings();
        OnPropertyChanged(nameof(MatrixUpmixing));
        OnPropertyChanged(nameof(CavernizeUpmixing));
        OnPropertyChanged(nameof(UpmixingEffect));
        OnPropertyChanged(nameof(UpmixingSmoothness));
    }

    public async Task RunQueue() {
        if (!CanRunQueue) {
            return;
        }

        IsBusy = true;
        Warning = "No warnings.";
        Status = "Rendering queue...";
        cancellation = new CancellationTokenSource();
        bool deleteActiveJobOutput = false;
        try {
            foreach (QueuedRenderJob job in QueueJobs.ToArray()) {
                cancellation.Token.ThrowIfCancellationRequested();
                activeJob = job;
                deleteActiveJobOutput = false;
                job.Status = "Opening";
                job.Progress = 0;
                await Task.Run(() => session.OpenContent(job.SourcePath), cancellation.Token);
                Tracks.Clear();
                foreach (CavernizeTrack track in session.LoadedFile.Tracks) {
                    Tracks.Add(track);
                }
                LoadedPath = job.SourcePath;
                LoadedTitle = Path.GetFileName(job.SourcePath);
                ApplyJobSettings(job);
                if (job.TrackIndex.HasValue && job.TrackIndex.Value >= 0 && job.TrackIndex.Value < session.LoadedFile.Tracks.Count) {
                    session.SelectedTrack = session.LoadedFile.Tracks[job.TrackIndex.Value];
                }
                SelectedTrack = session.SelectedTrack;

                job.Status = "Rendering";
                Status = $"Rendering {job.DisplayName}...";
                if (File.Exists(job.OutputPath) || QueueJobs.Any(other => !ReferenceEquals(other, job) &&
                    string.Equals(other.OutputPath, job.OutputPath, StringComparison.OrdinalIgnoreCase))) {
                    job.OutputPath = CreateOutputPath(job.SourcePath);
                }
                deleteActiveJobOutput = !File.Exists(job.OutputPath);
                await session.RenderAsync(job.OutputPath, cancellation.Token);
                deleteActiveJobOutput = false;
                job.Progress = 1;
                job.Status = "Done";
                ReportText = session.Report.Report;
                QueueJobs.Remove(job);
            }

            Status = "Queue completed.";
        } catch (OperationCanceledException) {
            if (activeJob != null) {
                activeJob.Status = "Canceled";
                if (deleteActiveJobOutput) {
                    File.Delete(activeJob.OutputPath);
                }
            }
            Status = "Queue canceled.";
            Warning = "No warnings.";
        } catch (Exception ex) {
            if (activeJob != null) {
                activeJob.Status = "Failed";
                if (deleteActiveJobOutput) {
                    File.Delete(activeJob.OutputPath);
                }
            }
            Status = "Queue failed.";
            Warning = ex.Message;
        } finally {
            activeJob = null;
            cancellation.Dispose();
            cancellation = null;
            IsProgressIndeterminate = false;
            IsBusy = false;
            OnPropertyChanged(nameof(CanRunQueue));
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

    public string GetPostRenderReportText() => string.IsNullOrWhiteSpace(ReportText) ? "No report yet." : ReportText;

    public void Dispose() {
        cancellation?.Cancel();
        cancellation?.Dispose();
        SaveSettings();
        session.Dispose();
    }

    QueuedRenderJob CreateJob(string sourcePath, int? trackIndex) => new(sourcePath, CreateOutputPath(sourcePath), trackIndex,
        SelectedExportFormat, SelectedRenderTarget, SpeakerVirtualizer, Force24Bit, MuteBed, MuteGround, SurroundSwap,
        WavChannelSkip, DetailedGrading, MatrixUpmixing, CavernizeUpmixing, UpmixingEffect, UpmixingSmoothness);

    string CreateOutputPath(string sourcePath) {
        HashSet<string> queuedOutputPaths = QueueJobs
            .Select(job => job.OutputPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string folder = Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory(),
            name = Path.GetFileNameWithoutExtension(sourcePath),
            extension = SuggestedOutputExtension;
        string candidate = Path.Combine(folder, $"{name}.cavernize.{extension}");
        for (int index = 2; File.Exists(candidate) || queuedOutputPaths.Contains(candidate); index++) {
            candidate = Path.Combine(folder, $"{name}.cavernize-{index}.{extension}");
        }
        return candidate;
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
        settings.ExportCodec = SelectedExportFormat?.Codec.ToString();
        settings.RenderTarget = SelectedRenderTarget?.Name;
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
            session.LoadRoomCorrection(settings.RoomCorrectionPath, new ConversionStrings());
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

    void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
