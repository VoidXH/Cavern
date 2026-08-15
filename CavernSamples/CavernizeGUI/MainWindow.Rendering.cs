using System;

using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Environment;
using Cavern.Format.Exceptions;
using Cavern.Format.Renderers;
using Cavern.Utilities;
using Cavern.Virtualizer;
using Cavern.WPF;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Resources;

namespace CavernizeGUI;

partial class MainWindow {
    /// <summary>
    /// Total number of samples for all channels that will be written to the file at once.
    /// </summary>
    int blockSize;

    /// <summary>
    /// Prepare the renderer for export.
    /// </summary>
    void PreRender() {
        if (taskEngine.IsOperationRunning) {
            throw new ConcurrencyException(language["OpRun"]);
        }
        if (tracks.SelectedItem == null) {
            throw new TrackException(language["LdSrc"]);
        }

        if (!((CavernizeTrack)tracks.SelectedItem).Supported) {
            throw new TrackException(language["UnTrk"]);
        }

        ExportFormat format = (ExportFormat)audio.SelectedItem;
        bool needsFFmpeg = !string.IsNullOrEmpty(format.FFName) && format.Codec != Codec.PCM_Float && format.Codec != Codec.PCM_LE;
        if (needsFFmpeg && !ffmpeg.Found) {
            throw new TrackException(language["FFOnl"]);
        }

        try {
            AttachToListener();
        } catch (OverMaxChannelsException e) {
            throw new TrackException(string.Format(language["ChCnt"], e.Channels, e.MaxChannels));
        }
    }

    /// <summary>
    /// Attach the track to the environment and perform compatibility checks.
    /// </summary>
    void AttachToListener() {
        try {
            environment.AttachToListener((CavernizeTrack)tracks.SelectedItem);
        } catch (NonGroundChannelPresentException) {
            throw new NonGroundChannelPresentException(language["SpViE"]);
        } catch (SampleRateMismatchException) {
            throw new IncompatibleSettingsException(language["FiltC"]);
        }
    }

    /// <summary>
    /// Start rendering to a target <paramref name="path"/>.
    /// </summary>
    /// <returns>A task for rendering or null when an error happened.</returns>
    Action Render(string path) {
        CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
        Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
        BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 : Settings.Default.force24Bit ? BitDepth.Int24 : BitDepth.Int16;

        if (codec.IsEnvironmental()) {
            try {
                EnvironmentWriter transcoder = EnvironmentWriter.Create(path, codec, environment.Listener, target.Length, bits, target.Renderer);
                return () => TranscodeTask(target, transcoder);
            } catch (UnsupportedContainerForWriteException) {
                Error(language["UnCod"]);
                return null;
            }
        }

        blockSize = CavernizeOutput.GetBlockSize(RenderTarget, environment);
        AudioWriter writer = CavernizeOutput.CreateRenderOutput(this, path, environment, target, codec, bits);
        return () => RenderTask(target, writer, path);
    }

    /// <summary>
    /// Create an external converter if it's needed for rendering a specific track.
    /// </summary>
    ExternalConverterHandler CreateExternalHandler(CavernizeTrack target, int keepFirstSources) {
        LicenceWindow licenceWindow = Dispatcher.Invoke(() => new LicenceWindow());
        ExternalConverterHandler external = new(target, licenceWindow, taskEngine.UpdateProgressBar, taskEngine.UpdateStatus, Dispatcher.Invoke);
        Dispatcher.Invoke(licenceWindow.Close);
        if (external.Failed) {
            Dispatcher.Invoke(() => Error(status.Text));
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

        taskEngine.Progress = 0;
        taskEngine.UpdateStatus(language["Start"]);
        RenderTarget renderTargetRef = Dispatcher.Invoke(() => RenderTarget);
        RenderStats stats = WriteRender(target, writer, renderTargetRef);
        report.Generate(stats);

        string targetCodec = null;
        audio.Dispatcher.Invoke(() => targetCodec = ((ExportFormat)audio.SelectedItem).FFName);

        if (writer is RIFFWaveWriter && finalName[^4..] != waveExtension) {
            taskEngine.UpdateStatus("Merging to final container...");
            string exportedAudio = finalName[..^4] + waveExtension;
            MergeToContainer merger = new(LoadedFile.Path, exportedAudio, targetCodec);
            merger.AddArguments(RenderingSettings.MergeArguments);
            merger.SetTrackName($"Cavern {renderTargetRef.Name} render");
            if (writer.ChannelCount > 8) {
                merger.Allow8PlusChannels();
            }
            merger.MakeSafe(finalName);
            if (!merger.Merge(ffmpeg, finalName)) {
                taskEngine.UpdateStatus("Failed to create the final file. Are your permissions sufficient in the export folder?");
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

        taskEngine.Progress = 0;
        taskEngine.UpdateStatus(language["Start"]);

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
        taskEngine.UpdateStatus(language["ExpOk"]);
        taskEngine.Progress = 1;

        if (Program.ConsoleMode) {
            Dispatcher.Invoke(Close);
        }

        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
            Error(language["JocWa"]);
        }
    }

    /// <summary>
    /// RIFF Wave file extension.
    /// </summary>
    const string waveExtension = ".wav";
}
