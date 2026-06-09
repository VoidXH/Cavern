using System.Numerics;

using Cavern;
using Cavern.CavernSettings;
using Cavern.Filters;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Environment;
using Cavern.Format.Renderers;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.CavernSettings;
using Cavernize.Logic.External;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using VoidX.WPF.FFmpeg;

namespace CavernizeAvalonia;

/// <summary>
/// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
/// </summary>
public sealed class CavernizeSession : ICavernizeApp, IDisposable {
    /// <inheritdoc/>
    public bool Rendering { get; private set; }

    /// <inheritdoc/>
    public AudioFile LoadedFile { get; private set; }

    /// <inheritdoc/>
    public CavernizeTrack SelectedTrack { get; set; }

    /// <inheritdoc/>
    public ExportFormat ExportFormat { get; set; }

    /// <inheritdoc/>
    public RenderTarget RenderTarget { get; set; }

    /// <inheritdoc/>
    public UpmixingSettings UpmixingSettings { get; }

    /// <inheritdoc/>
    public RenderingSettings RenderingSettings { get; }

    /// <inheritdoc/>
    public bool SurroundSwap { get; set; }

    /// <summary>
    /// Skip writing WAVEFORMATEXTENSIBLE channel masks for WAV output.
    /// </summary>
    public bool WavChannelSkip { get; set; }

    /// <summary>
    /// Generate detailed render grading statistics.
    /// </summary>
    public bool DetailedGrading { get; set; }

    /// <summary>
    /// Render without writing an output file, only creating the post-render report.
    /// </summary>
    public bool ReportMode { get; set; }

    /// <summary>
    /// FFmpeg runner and locator.
    /// </summary>
    public FFmpeg FFmpeg { get; }

    /// <summary>
    /// Licence prompt used by external converters.
    /// </summary>
    public ILicence LicencePrompt { get; set; } = new RejectingLicence();

    /// <summary>
    /// Last generated render report.
    /// </summary>
    public PostRenderReport Report { get; private set; }

    /// <summary>
    /// Raised when render progress changes. Progress can be -1 when it is indeterminate.
    /// </summary>
    public event Action<double> ProgressChanged;

    /// <summary>
    /// Raised when render status text changes.
    /// </summary>
    public event Action<string> StatusChanged;

    /// <summary>
    /// Raised for non-fatal warnings.
    /// </summary>
    public event Action<string> WarningRaised;

    readonly TrackStrings trackStrings;
    readonly RenderReportStrings reportStrings;
    readonly ExternalConverterStrings externalConverterStrings;
    readonly AvaloniaLanguage language;
    readonly ConversionEnvironment environment;
    CancellationToken cancellationToken;
    int blockSize;

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession() : this(null, null, null, null, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings) : this(null, trackStrings, reportStrings, externalConverterStrings, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(FFmpeg ffmpeg) : this(ffmpeg, null, null, null, null) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    internal CavernizeSession(AvaloniaLanguage language) :
        this(null, language.TrackStrings, language.RenderReportStrings, language.ExternalConverterStrings, language) { }

    /// <summary>
    /// UI-neutral Cavernize conversion session for command line and cross-platform frontends.
    /// </summary>
    public CavernizeSession(FFmpeg ffmpeg, TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings) : this(ffmpeg, trackStrings, reportStrings, externalConverterStrings, null) { }

    CavernizeSession(FFmpeg ffmpeg, TrackStrings trackStrings, RenderReportStrings reportStrings,
        ExternalConverterStrings externalConverterStrings, AvaloniaLanguage language) {
        this.trackStrings = trackStrings ?? new TrackStrings();
        this.reportStrings = reportStrings ?? new RenderReportStrings();
        this.externalConverterStrings = externalConverterStrings ?? new ExternalConverterStrings();
        this.language = language;

        ExportFormat = ExportFormat.GetFormats(this.trackStrings).First(format => format.Codec == Codec.PCM_LE);
        RenderTarget = RenderTarget.Targets.FirstOrDefault(target => target.Name == "5.1.2 side") ?? RenderTarget.Targets[0];
        UpmixingSettings = new UpmixingSettings(true);
        RenderingSettings = new RenderingSettings();
        FFmpeg = ffmpeg ?? StatusFFmpeg.Create(UpdateStatus);

        environment = new ConversionEnvironment(this);
        Report = new PostRenderReport(environment.Listener, this.reportStrings);
    }

    /// <inheritdoc/>
    public void OpenContent(string path) {
        Reset();
        FFmpeg.CheckFFmpeg();
        UpdateProgress(0);

        try {
            OpenContent(new AudioFile(path, trackStrings));
        } catch (Exception) {
            Reset();
            throw;
        }
    }

    /// <inheritdoc/>
    public void OpenContent(AudioFile file) {
        LoadedFile = file ?? throw new ArgumentNullException(nameof(file));
        if (file.Tracks.Count == 0) {
            throw new TrackException("No supported audio tracks were found in the file.");
        }

        SelectedTrack = file.Tracks
            .Where(track => track.Codec != Codec.Unknown)
            .OrderBy(track => track.Codec)
            .FirstOrDefault() ?? file.Tracks[0];
    }

    /// <inheritdoc/>
    public Action GetRenderTask(string path) {
        PreRender();
        CavernizeTrack target = SelectedTrack;
        Action renderTask;

        if (!ReportMode) {
            if (path == null) {
                throw new ArgumentNullException(nameof(path), "Output path is required outside report mode.");
            }
            renderTask = Render(path);
        } else {
            SetBlockSize(RenderTarget);
            renderTask = () => RenderTask(target, null, null);
        }

        return () => RunRendering(renderTask);
    }

    /// <inheritdoc/>
    public void RenderContent(string path) => GetRenderTask(path).Invoke();

    /// <summary>
    /// Start rendering on a background thread.
    /// </summary>
    public Task RenderAsync(string path) => RenderAsync(path, CancellationToken.None);

    /// <summary>
    /// Start rendering on a background thread.
    /// </summary>
    public Task RenderAsync(string path, CancellationToken cancellationToken) => Task.Run(() => {
        this.cancellationToken = cancellationToken;
        try {
            RenderContent(path);
        } finally {
            this.cancellationToken = default;
        }
    }, cancellationToken);

    /// <summary>
    /// Load a custom HRIR package for headphone virtualization.
    /// </summary>
    public void LoadHrir(string path) {
        using RIFFWaveReader file = new(path);
        file.ReadHeader();
        VirtualizerFilter.Override(
            VirtualChannel.Parse(new MultichannelWaveform(file.ReadMultichannelAfterHeader()), file.SampleRate),
            file.SampleRate);
    }

    /// <summary>
    /// Restore the built-in HRIR package for headphone virtualization.
    /// </summary>
    public void ResetHrir() => VirtualizerFilter.Reset();

    /// <inheritdoc/>
    public void Reset() {
        environment.Reset();
        LoadedFile?.Dispose();
        LoadedFile = null;
        SelectedTrack = null;
        Report = new PostRenderReport(environment.Listener, reportStrings);
        UpdateProgress(0);
    }

    /// <inheritdoc/>
    public void Dispose() => LoadedFile?.Dispose();

    void RunRendering(Action renderTask) {
        if (Rendering) {
            throw new ConcurrencyException("Another operation is already running.");
        }

        Rendering = true;
        try {
            ThrowIfCancellationRequested();
            renderTask();
        } finally {
            Rendering = false;
        }
    }

    void PreRender() {
        if (Rendering) {
            throw new ConcurrencyException("Another operation is already running.");
        }
        if (SelectedTrack == null) {
            throw new TrackException("Load a source track before rendering.");
        }
        if (!SelectedTrack.Supported) {
            throw new TrackException("The selected track is unsupported.");
        }

        bool needsFFmpeg = !string.IsNullOrEmpty(ExportFormat.FFName) &&
            ExportFormat.Codec != Codec.PCM_Float && ExportFormat.Codec != Codec.PCM_LE;
        if (needsFFmpeg && !FFmpeg.Found) {
            throw new TrackException("FFmpeg is required for the selected export format.");
        }

        AttachToListener();
    }

    void AttachToListener() {
        try {
            environment.AttachToListener(SelectedTrack);
        } catch (SampleRateMismatchException) {
            throw new IncompatibleSettingsException("The selected filters and render settings have incompatible sample rates.");
        }
    }

    Action Render(string path) {
        CavernizeTrack target = SelectedTrack;
        Codec codec = ExportFormat.Codec;
        BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 :
            RenderingSettings.Force24Bit ? BitDepth.Int24 : BitDepth.Int16;

        if (codec.IsEnvironmental()) {
            EnvironmentWriter transcoder = EnvironmentWriter.Create(path, codec, environment.Listener, target.Length, bits,
                target.Renderer);
            return () => TranscodeTask(target, transcoder);
        }

        SetBlockSize(RenderTarget);
        string exportFormat = Path.GetExtension(path).ToLowerInvariant();
        AudioWriter writer = CreateChannelWriter(target, path, exportFormat, codec, bits) ??
            throw new UnsupportedContainerForWriteException(exportFormat);

        writer.WriteHeader();
        return () => RenderTask(target, writer, path);
    }

    AudioWriter CreateChannelWriter(CavernizeTrack target, string outputPath, string extension, Codec codec, BitDepth bits) {
        bool writeIntoSourceContainer = extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) &&
            target.Container == Container.Matroska &&
            (codec == Codec.PCM_LE || codec == Codec.PCM_Float);
        if (writeIntoSourceContainer) {
            return new AudioWriterIntoContainer(outputPath, target.GetVideoTracks(), codec, blockSize,
                RenderTarget.OutputChannels, target.Length, target.SampleRate, bits) {
                NewTrackName = $"Cavern {RenderTarget.Name} render"
            };
        }

        int channels = RenderTarget.OutputChannels;
        string audioPath = extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) || extension.IsNative() ?
            Path.ChangeExtension(outputPath, waveExtension) :
            outputPath;
        return extension.Equals(waveExtension, StringComparison.OrdinalIgnoreCase) && !WavChannelSkip ?
            new RIFFWaveWriter(audioPath, RenderTarget.Channels[..channels], target.Length, environment.Listener.SampleRate, bits) :
            AudioWriter.Create(audioPath, channels, target.Length, environment.Listener.SampleRate, bits);
    }

    void SetBlockSize(RenderTarget target) {
        int updateRate = environment.Listener.UpdateRate;
        int samples = RenderingSettings.RoomCorrectionUsable ? RenderingSettings.RoomCorrection.Samples : defaultWriteCacheLength;
        int alignedSamples = Math.Max(samples, updateRate);
        int extraSamples = alignedSamples % updateRate;
        if (extraSamples != 0) {
            alignedSamples += updateRate - extraSamples;
        }
        blockSize = alignedSamples * target.OutputChannels;
    }

    ExternalConverterHandler CreateExternalHandler(CavernizeTrack target, int keepFirstSources) {
        ExternalConverterHandler external = new(target, externalConverterStrings, LicencePrompt, UpdateProgress, UpdateStatus,
            action => action());
        if (!external.Failed) {
            external.Attach(environment.Listener, UpmixingSettings, keepFirstSources);
        }
        return external;
    }

    void RenderTask(CavernizeTrack target, AudioWriter writer, string finalName) {
        using (ExternalConverterHandler external = CreateExternalHandler(target, 0)) {
            if (external.Failed) {
                return;
            }

            ThrowIfCancellationRequested();
            UpdateProgress(0);
            UpdateStatus("Rendering...");
            RenderTarget renderTargetRef = RenderTarget;
            RenderStats stats = WriteRender(target, writer, renderTargetRef);
            Report.Generate(stats);

            if (NeedsFinalContainerMerge(writer, finalName) && !MergeRenderedWave(finalName, writer.ChannelCount, renderTargetRef)) {
                return;
            }
        }
        FinishTask(target);
    }

    bool NeedsFinalContainerMerge(AudioWriter writer, string finalName) =>
        writer is RIFFWaveWriter &&
        finalName != null &&
        !Path.GetExtension(finalName).Equals(waveExtension, StringComparison.OrdinalIgnoreCase);

    bool MergeRenderedWave(string finalName, int channelCount, RenderTarget renderTarget) {
        UpdateStatus("Merging to final container...");
        MergeToContainer merger = CreateContainerMerger(finalName, channelCount, renderTarget);
        bool merged = merger.Merge(FFmpeg, finalName);
        if (!merged) {
            UpdateStatus("Failed to create the final file. Are your permissions sufficient in the export folder?");
        }
        return merged;
    }

    MergeToContainer CreateContainerMerger(string finalName, int channelCount, RenderTarget renderTarget) {
        MergeToContainer merger = new(LoadedFile.Path, Path.ChangeExtension(finalName, waveExtension), ExportFormat.FFName);
        merger.AddArguments(RenderingSettings.MergeArguments);
        merger.SetTrackName($"Cavern {renderTarget.Name} render");
        if (channelCount > 8) {
            merger.Allow8PlusChannels();
        }
        merger.MakeSafe(finalName);
        return merger;
    }

    void TranscodeTask(CavernizeTrack target, EnvironmentWriter writer) {
        if (writer is DolbyAtmosBWFWriter bwfWriter) {
            bwfWriter.ExtendWithMuteTarget();
        }
        using (ExternalConverterHandler external = CreateExternalHandler(target, writer is DolbyAtmosBWFWriter ? 10 : 0)) {
            if (external.Failed) {
                return;
            }

            ThrowIfCancellationRequested();
            UpdateProgress(0);
            UpdateStatus("Rendering...");

            RenderStats stats = writer is BroadcastWaveFormatWriter bwf ? WriteTranscode(target, bwf) : WriteTranscode(target, writer);
            Report.Generate(stats);
        }
        FinishTask(target);
    }

    void FinishTask(CavernizeTrack target) {
        UpdateStatus(language == null ? "Finished!" : language["ExpOk"]);
        UpdateProgress(1);

        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
            WarningRaised?.Invoke("Enhanced AC-3 metadata required a compatibility workaround during rendering.");
        }
    }

    /// <summary>
    /// Keeps track of export time and evaluates performance.
    /// </summary>
    class Progressor(long length, Listener listener, AvaloniaLanguage language, Action<double> updateProgress,
        Action<string> updateStatus) {
        /// <summary>
        /// Samples rendered so far.
        /// </summary>
        public long Rendered { get; private set; }

        /// <summary>
        /// Time of starting the export process.
        /// </summary>
        readonly DateTime start = DateTime.Now;

        /// <summary>
        /// Multiplier of the content length to get the progress ratio.
        /// </summary>
        readonly double samplesToProgress = 1.0 / length;

        /// <summary>
        /// Converts samples to seconds.
        /// </summary>
        readonly double samplesToSeconds = 1.0 / listener.SampleRate;

        /// <summary>
        /// Samples processed each frame.
        /// </summary>
        readonly long updateRate = listener.UpdateRate;

        readonly string progressStatus = language == null ?
            "Rendering... ({0}, speed: {1}x, remaining: {2})" :
            language["ProgP"];
        readonly string finalizingStatus = language == null ? "Finalizing... ({0})" : language["FinaP"];

        /// <summary>
        /// Samples until next UI update.
        /// </summary>
        long untilUpdate = updateInterval;

        /// <summary>
        /// Report progress after each listener update.
        /// </summary>
        public void Update() {
            Rendered += updateRate;
            if ((untilUpdate -= updateRate) <= 0) {
                double progress = Rendered * samplesToProgress;
                TimeSpan elapsed = DateTime.Now - start, remaining = elapsed / progress - elapsed;
                double speed = Rendered * samplesToSeconds / elapsed.TotalSeconds;

                string remDisp;
                if (remaining.TotalDays < 1) {
                    if (remaining.TotalHours < 1) {
                        remDisp = remaining.ToString("mm':'ss");
                    } else {
                        remDisp = remaining.ToString("h':'mm':'ss");
                    }
                } else {
                    remDisp = remaining.ToString("d':'hh':'mm':'ss");
                }

                updateStatus?.Invoke(string.Format(progressStatus, progress.ToString("0.00%"), speed.ToString("0.00"), remDisp));
                updateProgress?.Invoke(progress);
                untilUpdate = updateInterval;
            }
        }

        /// <summary>
        /// Report custom progress as finalization.
        /// </summary>
        public void Finalize(double progress) {
            updateStatus?.Invoke(string.Format(finalizingStatus, progress.ToString("0.00%")));
            updateProgress?.Invoke(progress);
        }
    }

    RenderStats WriteRender(CavernizeTrack target, AudioWriter writer, RenderTarget renderTarget) {
        RenderStats stats = DetailedGrading ? new RenderStatsEx(environment.Listener) : new RenderStats(environment.Listener);
        Progressor progressor = new(target.Length, environment.Listener, language, UpdateProgress, UpdateStatus);
        bool customMuting = RenderingSettings.MuteBed || RenderingSettings.MuteGround;

        MultichannelConvolver filters = null;
        if (RenderingSettings.RoomCorrectionUsable) {
            filters = new MultichannelConvolver(RenderingSettings.RoomCorrection.Data);
        }

        // Virtualization is done with the buffer instead of each update in the listener to optimize FFT sizes.
        VirtualizerFilter virtualizer = null;
        Normalizer normalizer = null;
        bool virtualizerState = Listener.HeadphoneVirtualizer;
        if (virtualizerState || RenderingSettings.SpeakerVirtualizer) {
            Listener.HeadphoneVirtualizer = false;
            virtualizer = new VirtualizerFilter();
            virtualizer.SetLayout();
            normalizer = new Normalizer(true) {
                decayFactor = 10 * (float)environment.Listener.UpdateRate / environment.Listener.SampleRate
            };
        }

        int cachePosition = 0;
        bool flush = false;
        // The size of writeCache makes sure ContainerWriters get correct sized blocks when written with WriteChannelLimitedBlock.
        float[] writeCache = new float[blockSize / renderTarget.OutputChannels * Listener.Channels.Length];

#if RELEASE
        bool wasError = false;
#endif
        try {
            while (progressor.Rendered < target.Length) {
                ThrowIfCancellationRequested();
                float[] result;
#if RELEASE
                try {
#endif
                    result = environment.Listener.Render();
#if RELEASE
                } catch (Exception e) {
                    if (!wasError) {
                        wasError = true;
                        TimeSpan time = TimeSpan.FromSeconds(progressor.Rendered / environment.Listener.SampleRate);
                        string renderError = language == null ?
                            "The audio at {0} could not be rendered. Processing will continue, but the track won't be intact after that timestamp. The exact error: {1}" :
                            language["RenEr"];
                        WarningRaised?.Invoke(string.Format(renderError, time, e.Message));
                    }
                    result = new float[Listener.Channels.Length * environment.Listener.UpdateRate];
                }
#endif

                // Alignment of split parts.
                if (target.Length - progressor.Rendered < environment.Listener.UpdateRate) {
                    Array.Resize(ref result, (int)((target.Length - progressor.Rendered) * Listener.Channels.Length));
                    flush = true;
                }

                Array.Copy(result, 0, writeCache, cachePosition, result.Length);
                cachePosition += result.Length;
                if (cachePosition == writeCache.Length || flush) {
                    filters?.Process(writeCache);

                    if (virtualizer == null) {
                        if (renderTarget is not DownmixedRenderTarget downmix) {
                            writer?.WriteBlock(writeCache, 0, cachePosition);
                        } else {
                            downmix.PerformMerge(writeCache);
                            writer?.WriteChannelLimitedBlock(writeCache, downmix.OutputChannels,
                                Listener.Channels.Length, 0, cachePosition);
                        }
                    } else {
                        virtualizer.Process(writeCache, environment.Listener.SampleRate);
                        normalizer.Process(writeCache);
                        writer?.WriteChannelLimitedBlock(writeCache, renderTarget.OutputChannels,
                            Listener.Channels.Length, 0, cachePosition);
                    }
                    cachePosition = 0;
                }

                if (progressor.Rendered > secondFrame) {
                    stats.Update(result);
                }

                if (customMuting) {
                    ApplyObjectMuting(target.Renderer.Objects);
                }

                progressor.Update();
            }
        } finally {
            writer?.Dispose();
            if (virtualizerState) {
                Listener.HeadphoneVirtualizer = true;
            }
        }

        return stats;
    }

    void ApplyObjectMuting(IReadOnlyList<Source> objects) {
        foreach (Source source in objects) {
            Vector3 position = source.Position / Listener.EnvironmentSize;
            source.Mute =
                (RenderingSettings.MuteBed && MathF.Abs(position.X) % 1 < .01f &&
                    MathF.Abs(position.Y) % 1 < .01f && MathF.Abs(position.Z % 1) < .01f) ||
                (RenderingSettings.MuteGround && position.Y == 0);
        }
    }

    RenderStats WriteTranscode(CavernizeTrack target, EnvironmentWriter writer) {
        RenderStats stats = new(environment.Listener);
        Progressor progressor = new(target.Length, environment.Listener, language, UpdateProgress, UpdateStatus);

        while (progressor.Rendered < target.Length) {
            ThrowIfCancellationRequested();
            writer.WriteNextFrame();
            progressor.Update();
        }

        writer.Dispose();
        return stats;
    }

    RenderStats WriteTranscode(CavernizeTrack target, BroadcastWaveFormatWriter writer) {
        RenderStats stats = new(environment.Listener);
        Progressor progressor = new((long)(target.Length / progressSplit), environment.Listener, language, UpdateProgress,
            UpdateStatus);

        while (progressor.Rendered < target.Length) {
            ThrowIfCancellationRequested();
            writer.WriteNextFrame();
            progressor.Update();
        }

        writer.FinalFeedback = progressor.Finalize;
        writer.FinalFeedbackStart = progressSplit;
        writer.Dispose();
        return stats;
    }

    void UpdateStatus(string text) => StatusChanged?.Invoke(text);

    void UpdateProgress(double progress) => ProgressChanged?.Invoke(progress);

    void ThrowIfCancellationRequested() => cancellationToken.ThrowIfCancellationRequested();

    const string waveExtension = ".wav";
    const int defaultWriteCacheLength = 16384;
    const int secondFrame = 2 * 1536;
    const double progressSplit = .95;
    const long updateInterval = 50000;
}
