using Avalonia.Input;
using Avalonia.Platform.Storage;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Environment;
using Cavern.Format.Renderers;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Avalonia;
using Cavernize.Logic.External;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;

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
            RenderTarget selected = await new RenderTargetSelectorWindow(RenderTargetLabel.TrimEnd(':'),
                RenderTargetSelectorText("PCRea"), RenderTargetSelectorText("Matri"),
                RenderTargetSelectorText("MulCH"), RenderTargets, SelectedRenderTarget).ShowDialog<RenderTarget>(this);
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
            throw new ConcurrencyException((string)language["OpRun"]);
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

    /// <summary>
    /// Prepare the renderer for export.
    /// </summary>
    void PreRender() {
        if (Rendering) {
            throw new ConcurrencyException((string)language["OpRun"]);
        }
        if (SelectedTrack == null) {
            throw new TrackException((string)language["LdSrc"]);
        }
        if (!SelectedTrack.Supported) {
            throw new TrackException((string)language["UnTrk"]);
        }

        ExportFormat format = ExportFormat;
        bool needsFFmpeg = !string.IsNullOrEmpty(format.FFName) && format.Codec != Codec.PCM_Float && format.Codec != Codec.PCM_LE;
        if (needsFFmpeg && !ffmpeg.Found) {
            throw new TrackException((string)language["FFOnl"]);
        }

        try {
            AttachToListener();
        } catch (OverMaxChannelsException e) {
            throw new TrackException(string.Format((string)language["ChCnt"], e.Channels, e.MaxChannels));
        }
    }

    /// <summary>
    /// Attach the track to the environment and perform compatibility checks.
    /// </summary>
    void AttachToListener() {
        try {
            environment.AttachToListener(SelectedTrack);
        } catch (NonGroundChannelPresentException) {
            throw new NonGroundChannelPresentException((string)language["SpViE"]);
        } catch (SampleRateMismatchException) {
            throw new IncompatibleSettingsException((string)language["FiltC"]);
        }
    }

    /// <summary>
    /// Start rendering to a target <paramref name="path"/>.
    /// </summary>
    /// <returns>A task for rendering or null when an error happened.</returns>
    Action Render(string path) {
        CavernizeTrack target = SelectedTrack;
        Codec codec = ExportFormat.Codec;
        BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 : RenderingSettings.Force24Bit ? BitDepth.Int24 : BitDepth.Int16;

        if (codec.IsEnvironmental()) {
            try {
                EnvironmentWriter transcoder = EnvironmentWriter.Create(path, codec, environment.Listener, target.Length, bits, target.Renderer);
                return () => TranscodeTask(target, transcoder);
            } catch (UnsupportedContainerForWriteException) {
                Error((string)language["UnCod"]);
                return null;
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
            Error((string)language["UnExt"]);
            return null;
        }
        writer.WriteHeader();
        return () => RenderTask(target, writer, path);
    }

    /// <summary>
    /// Setup write cache block size depending on active settings.
    /// </summary>
    void SetBlockSize(RenderTarget target) {
        int updateRate = environment.Listener.UpdateRate;
        blockSize = RenderingSettings.RoomCorrectionUsable ? RenderingSettings.RoomCorrection.Samples : defaultWriteCacheLength;
        if (blockSize < updateRate) {
            blockSize = updateRate;
        } else if (blockSize % updateRate != 0) {
            // Cache handling is written to only handle when its size is divisible with the update rate - it's faster this way
            blockSize += updateRate - blockSize % updateRate;
        }
        blockSize *= target.OutputChannels;
    }

    /// <summary>
    /// Create an external converter if it's needed for rendering a specific track.
    /// </summary>
    ExternalConverterHandler CreateExternalHandler(CavernizeTrack target, int keepFirstSources) {
        ILicence licenceWindow = Program.ConsoleMode ?
            new RejectingLicence() :
            new LicenceWindow(this, Text("OK"), Text("Cancel"));
        string externalStatus = null;
        void UpdateExternalStatus(string text) {
            externalStatus = text;
            UpdateStatus(text);
        }

        ExternalConverterHandler external = new(target, language.ExternalConverterStrings, licenceWindow,
            UpdateProgress, UpdateExternalStatus, action => action());
        if (external.Failed) {
            Error(externalStatus ?? Status);
        } else {
            external.Attach(environment.Listener, new DynamicUpmixingSettings(), keepFirstSources);
        }
        return external;
    }

    /// <summary>
    /// Render the content and export it to a channel-based format.
    /// </summary>
    void RenderTask(CavernizeTrack target, AudioWriter writer, string finalName) {
        ExternalConverterHandler external = CreateExternalHandler(target, 0);
        if (external.Failed) {
            return;
        }
        ThrowIfCancellationRequested();
        UpdateProgress(0);
        UpdateStatus((string)language["Start"]);
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

    /// <summary>
    /// Decode the source and export it to an object-based format.
    /// </summary>
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
        UpdateStatus((string)language["Start"]);

        RenderStats stats;
        if (writer is BroadcastWaveFormatWriter bwf) {
            stats = WriteTranscode(target, bwf);
        } else {
            stats = WriteTranscode(target, writer);
        }
        report.Generate(stats);
        external.Dispose();
        FinishTask(target);
    }

    /// <summary>
    /// Operations to perform after a conversion was successful.
    /// </summary>
    void FinishTask(CavernizeTrack target) {
        UpdateStatus((string)language["ExpOk"]);
        UpdateProgress(1);

        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
            WarningRaised((string)language["JocWa"]);
        }
    }

    sealed class RejectingLicence : ILicence {
        public string Description { get; private set; }

        public string LicenceText { get; private set; }

        public void SetDescription(string description) => Description = description;

        public void SetLicenceText(string licence) => LicenceText = licence;

        public bool Prompt() => false;
    }

    /// <summary>
    /// RIFF Wave file extension.
    /// </summary>
    const string waveExtension = ".wav";

    /// <summary>
    /// Default value of <see cref="blockSize"/> per channel.
    /// </summary>
    const int defaultWriteCacheLength = 16384;
}
