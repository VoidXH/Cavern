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
public sealed partial class CavernizeSession {
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

    void RunRendering(Action renderTask) {
        if (Rendering) {
            throw new ConcurrencyException(language["OpRun"]);
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
        if (needsFFmpeg && !FFmpeg.Found) {
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
            // Cache handling is written to only handle when its size is divisible with the update rate - it's faster this way
            blockSize += updateRate - blockSize % updateRate;
        }
        blockSize *= target.OutputChannels;
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
        ExternalConverterHandler external = CreateExternalHandler(target, 0);
        if (external.Failed) {
            return;
        }
        ThrowIfCancellationRequested();
        UpdateProgress(0);
        UpdateStatus(language["Start"]);
        RenderTarget renderTargetRef = RenderTarget;
        RenderStats stats = WriteRender(target, writer, renderTargetRef);
        Report.Generate(stats);

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
            if (!merger.Merge(FFmpeg, finalName)) {
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
        Report.Generate(stats);
        external.Dispose();
        FinishTask(target);
    }

    void FinishTask(CavernizeTrack target) {
        UpdateStatus(language["ExpOk"]);
        UpdateProgress(1);

        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
            WarningRaised?.Invoke(language["JocWa"]);
        }
    }
}
