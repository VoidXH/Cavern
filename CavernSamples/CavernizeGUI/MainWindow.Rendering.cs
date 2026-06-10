using Avalonia.Input;
using Avalonia.Platform.Storage;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Environment;
using Cavern.Format.Renderers;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.External;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;

namespace CavernizeGUI;

// Functions that prepare and run the render process.
partial class MainWindow {
    bool renderTargetSelectorOpen;

    async void OpenFile(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        string[] paths = await PickFilePaths(new FilePickerOpenOptions {
            Title = OpenSourcePickerTitle,
            AllowMultiple = true,
            SuggestedStartLocation = await GetStartFolder(LastDirectory),
            FileTypeFilter = [
                new FilePickerFileType(AudioVideoFileType) {
                    Patterns = Cavern.Format.AudioReader.filter.Split(';')
                },
                FilePickerFileTypes.All
            ]
        });
        if (paths.Length == 1) {
            await OpenFile(paths[0]);
        } else if (paths.Length > 1) {
            await AddFilesToQueue(paths);
        }
    }

    async void OnRenderTargetOpened(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        if (renderTargetSelectorOpen) {
            return;
        }

        renderTargetSelectorOpen = true;
        try {
            RenderTarget selected = await new RenderTargetSelectorWindow(this).ShowDialog<RenderTarget>(this);
            if (selected != null) {
                SelectedRenderTarget = selected;
            }
        } finally {
            renderTargetSelectorOpen = false;
        }
    }

    async void LocateFFmpeg(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
        string path = await PickSingleFilePath(new FilePickerOpenOptions {
            Title = Text("FFLoc"),
            AllowMultiple = false,
            SuggestedStartLocation = await GetStartFolder(LastDirectory),
            FileTypeFilter = [FilePickerFileTypes.All]
        });
        if (!string.IsNullOrWhiteSpace(path)) {
            SetFfmpegLocation(path);
        }
    }

    async Task<IStorageFolder> GetStartFolder(string path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ?
            await StorageProvider.TryGetFolderFromPathAsync(path) :
            null;

    async Task<string> PickSingleFilePath(FilePickerOpenOptions options) {
        if (StorageProvider == null) {
            return null;
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

    async Task<string[]> PickFilePaths(FilePickerOpenOptions options) {
        if (StorageProvider == null) {
            return [];
        }

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        return [.. files.Select(file => file.Path.LocalPath).Where(path => !string.IsNullOrWhiteSpace(path))];
    }

    async Task<string> PickSingleFolderPath(FolderPickerOpenOptions options) {
        if (StorageProvider == null) {
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(options);
        return folders.Count == 1 ? folders[0].Path.LocalPath : null;
    }

    void FileDragEnter(object sender, DragEventArgs e) {
        if (e.DataTransfer?.TryGetFiles() != null) {
            e.DragEffects = DragDropEffects.Copy;
        }
    }

    void FileDragOver(object sender, DragEventArgs e) => e.Handled = true;

    void RunRendering(Action renderTask) {
        if (Rendering) {
            throw new ConcurrencyException(language["OpRun"]);
        }

        rendering = true;
        OnPropertyChanged(nameof(Rendering));
        try {
            ThrowIfCancellationRequested();
            renderTask();
        } finally {
            rendering = false;
            OnPropertyChanged(nameof(Rendering));
        }
    }

    void PreRender() {
        if (Rendering) {
            throw new ConcurrencyException(language["OpRun"]);
        }
        if (SelectedTrack == null) {
            throw new TrackException(language["LdSrc"]);
        }
        if (!SelectedTrack.Supported) {
            throw new TrackException(language["UnTrk"]);
        }

        bool needsFFmpeg = !string.IsNullOrEmpty(ExportFormat.FFName) &&
            ExportFormat.Codec != Codec.PCM_Float && ExportFormat.Codec != Codec.PCM_LE;
        if (needsFFmpeg && !ffmpeg.Found) {
            throw new TrackException(language["FFOnl"]);
        }

        try {
            AttachToListener();
        } catch (OverMaxChannelsException e) {
            throw new TrackException(string.Format(language["ChCnt"], e.Channels, e.MaxChannels));
        }
    }

    void AttachToListener() {
        try {
            environment.AttachToListener(SelectedTrack);
        } catch (NonGroundChannelPresentException) {
            throw new NonGroundChannelPresentException(language["SpViE"]);
        } catch (SampleRateMismatchException) {
            throw new IncompatibleSettingsException(language["FiltC"]);
        }
    }

    Action Render(string path) {
        CavernizeTrack target = SelectedTrack;
        Codec codec = ExportFormat.Codec;
        BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 :
            RenderingSettings.Force24Bit ? BitDepth.Int24 : BitDepth.Int16;

        if (codec.IsEnvironmental()) {
            try {
                EnvironmentWriter transcoder = EnvironmentWriter.Create(path, codec, environment.Listener, target.Length, bits,
                    target.Renderer);
                return () => TranscodeTask(target, transcoder);
            } catch (UnsupportedContainerForWriteException) {
                throw new TrackException(language["UnCod"]);
            }
        }

        SetBlockSize(RenderTarget);
        string exportFormat = path[^4..].ToLowerInvariant();
        bool mkvTarget = exportFormat.Equals(".mkv");
        string exportName = mkvTarget || exportFormat.IsNative() ?
            path[..^4] + waveExtension :
            path;
        int channelCount = RenderTarget.OutputChannels;
        AudioWriter writer;
        if (mkvTarget && target.Container == Container.Matroska && (codec == Codec.PCM_LE || codec == Codec.PCM_Float)) {
            writer = new AudioWriterIntoContainer(path, target.GetVideoTracks(), codec,
                blockSize, channelCount, target.Length, target.SampleRate, bits) {
                NewTrackName = $"Cavern {RenderTarget.Name} render"
            };
        } else if (exportFormat.Equals(waveExtension) && !WavChannelSkip) {
            writer = new RIFFWaveWriter(exportName, RenderTarget.Channels[..channelCount],
                target.Length, environment.Listener.SampleRate, bits);
        } else {
            writer = AudioWriter.Create(exportName, channelCount, target.Length, environment.Listener.SampleRate, bits);
        }
        if (writer == null) {
            throw new TrackException(language["UnExt"]);
        }
        writer.WriteHeader();
        return () => RenderTask(target, writer, path);
    }

    void SetBlockSize(RenderTarget target) {
        int updateRate = environment.Listener.UpdateRate;
        blockSize = RenderingSettings.RoomCorrectionUsable ? RenderingSettings.RoomCorrection.Samples : defaultWriteCacheLength;
        if (blockSize < updateRate) {
            blockSize = updateRate;
        } else if (blockSize % updateRate != 0) {
            // Cache handling is faster when the cache size is divisible by the listener update rate.
            blockSize += updateRate - blockSize % updateRate;
        }
        blockSize *= target.OutputChannels;
    }

    ExternalConverterHandler CreateExternalHandler(CavernizeTrack target, int keepFirstSources) {
        ExternalConverterHandler external = new(target, language.ExternalConverterStrings, new RejectingLicence(),
            UpdateProgress, UpdateStatus, action => action());
        if (!external.Failed) {
            external.Attach(environment.Listener, UpmixingSettings, keepFirstSources);
        }
        return external;
    }

    void RenderTask(CavernizeTrack target, AudioWriter writer, string finalName) {
        ExternalConverterHandler external = CreateExternalHandler(target, 0);
        if (external.Failed) {
            return;
        }
        ThrowIfCancellationRequested();
        UpdateProgress(0);
        UpdateStatus(language["Start"]);
        RenderTarget renderTargetRef = RenderTarget;
        RenderStats stats = WriteRender(target, writer, renderTargetRef);
        report.Generate(stats);

        string targetCodec = ExportFormat.FFName;
        if (writer is RIFFWaveWriter && finalName[^4..] != waveExtension) {
            UpdateStatus("Merging to final container...");
            string exportedAudio = finalName[..^4] + waveExtension;
            MergeToContainer merger = new(LoadedFile.Path, exportedAudio, targetCodec);
            merger.AddArguments(RenderingSettings.MergeArguments);
            merger.SetTrackName($"Cavern {renderTargetRef.Name} render");
            if (writer.ChannelCount > 8) {
                merger.Allow8PlusChannels();
            }
            merger.MakeSafe(finalName);
            if (!merger.Merge(ffmpeg, finalName)) {
                UpdateStatus("Failed to create the final file. Are your permissions sufficient in the export folder?");
                external.Dispose();
                return;
            }
        }

        external.Dispose();
        FinishTask(target);
    }

    void TranscodeTask(CavernizeTrack target, EnvironmentWriter writer) {
        if (writer is DolbyAtmosBWFWriter bwfWriter) {
            bwfWriter.ExtendWithMuteTarget();
        }
        ExternalConverterHandler external = CreateExternalHandler(target, writer is DolbyAtmosBWFWriter ? 10 : 0);
        if (external.Failed) {
            return;
        }
        ThrowIfCancellationRequested();
        UpdateProgress(0);
        UpdateStatus(language["Start"]);

        RenderStats stats = writer is BroadcastWaveFormatWriter bwf ? WriteTranscode(target, bwf) : WriteTranscode(target, writer);
        report.Generate(stats);
        external.Dispose();
        FinishTask(target);
    }

    void FinishTask(CavernizeTrack target) {
        UpdateStatus(language["ExpOk"]);
        UpdateProgress(1);

        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
            WarningRaised(language["JocWa"]);
        }
    }

    sealed class RejectingLicence : ILicence {
        public string Description { get; private set; }

        public string LicenceText { get; private set; }

        public void SetDescription(string description) => Description = description;

        public void SetLicenceText(string licence) => LicenceText = licence;

        public bool Prompt() => false;
    }

    const string waveExtension = ".wav";
    const int defaultWriteCacheLength = 16384;
}
