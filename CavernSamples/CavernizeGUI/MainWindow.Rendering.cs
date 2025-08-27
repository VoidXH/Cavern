using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Container;
using Cavern.Format.Environment;
using Cavern.Format.Renderers;
using Cavern.Utilities;
using Cavern.Virtualizer;
using Cavern.WPF;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <inheritdoc/>
        public float RenderGain { get; set; } = 1;

        /// <summary>
        /// Total number of samples for all channels that will be written to the file at once.
        /// </summary>
        int blockSize;

        /// <summary>
        /// Prepare the renderer for export.
        /// </summary>
        void PreRender() {
            if (taskEngine.IsOperationRunning) {
                throw new ConcurrencyException((string)language["OpRun"]);
            }
            if (tracks.SelectedItem == null) {
                throw new TrackException((string)language["LdSrc"]);
            }

            if (!((CavernizeTrack)tracks.SelectedItem).Supported) {
                throw new TrackException((string)language["UnTrk"]);
            }

            ExportFormat format = (ExportFormat)audio.SelectedItem;
            bool needsFFmpeg = !string.IsNullOrEmpty(format.FFName) && format.Codec != Codec.PCM_Float && format.Codec != Codec.PCM_LE;
            if (needsFFmpeg && !ffmpeg.Found) {
                throw new TrackException((string)language["FFOnl"]);
            }

            try {
                SoftPreRender();
            } catch (OverMaxChannelsException e) {
                throw new TrackException(string.Format((string)language["ChCnt"], e.Channels, e.MaxChannels));
            }
        }

        /// <summary>
        /// Prepare the renderer for export, without safety checks.
        /// </summary>
        void SoftPreRender() {
            CavernizeTrack track = (CavernizeTrack)tracks.SelectedItem;
            try {
                environment.AttachToListener(track, Settings.Default.surroundSwap);
            } catch (NonGroundChannelPresentException) {
                throw new NonGroundChannelPresentException((string)language["SpViE"]);
            }

            if (RenderTarget is VirtualizerRenderTarget) {
                if (roomCorrection != null && roomCorrectionSampleRate != VirtualizerFilter.FilterSampleRate) {
                    throw new IncompatibleSettingsException((string)language["FiltC"]);
                }
                environment.Listener.SampleRate = VirtualizerFilter.FilterSampleRate;
            } else {
                environment.Listener.SampleRate = roomCorrection == null ? track.SampleRate : roomCorrectionSampleRate;
            }
        }

        /// <summary>
        /// Start rendering to a target <paramref name="path"/>.
        /// </summary>
        /// <returns>A task for rendering or null when an error happened.</returns>
        Action Render(string path) {
            CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
            Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
            BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 : force24Bit.IsChecked ? BitDepth.Int24 : BitDepth.Int16;
            if (!codec.IsEnvironmental()) {
                SetBlockSize(RenderTarget);
                string exportFormat = path[^4..].ToLowerInvariant();
                bool mkvTarget = exportFormat.Equals(".mkv");
                string exportName = mkvTarget || exportFormat.Equals(".ac3") || exportFormat.Equals(".ec3") ?
                    path[..^4] + waveExtension :
                    path;
                int channelCount = RenderTarget.OutputChannels;
                AudioWriter writer;
                if (mkvTarget && target.Container == Container.Matroska && (codec == Codec.PCM_LE || codec == Codec.PCM_Float)) {
                    writer = new AudioWriterIntoContainer(path, target.GetVideoTracks(), codec,
                        blockSize, channelCount, target.Length, target.SampleRate, bits) {
                        NewTrackName = $"Cavern {RenderTarget.Name} render"
                    };
                } else if (exportFormat.Equals(waveExtension) && !wavChannelSkip.IsChecked) {
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
            } else {
                EnvironmentWriter transcoder;
                switch (codec) {
                    case Codec.LimitlessAudio:
                        transcoder = new LimitlessAudioFormatEnvironmentWriter(path, environment.Listener, target.Length, bits);
                        break;
                    case Codec.ADM_BWF:
                        transcoder = new BroadcastWaveFormatWriter(path, environment.Listener, target.Length, bits);
                        break;
                    case Codec.ADM_BWF_Atmos:
                        transcoder = new DolbyAtmosBWFWriter(path, environment.Listener, target.Length, bits, target.Renderer);
                        break;
                    case Codec.DAMF:
                        transcoder = new DolbyAtmosMasterFormatWriter(path, environment.Listener, target.Length, bits, target.Renderer);
                        break;
                    default:
                        Error((string)language["UnCod"]);
                        return null;
                }
                return () => TranscodeTask(target, transcoder);
            }
        }

        /// <summary>
        /// Get the render task after an output file was selected if export is selected.
        /// </summary>
        /// <returns>A task for rendering or null when an error happened.</returns>
        Action GetRenderTask() => GetRenderTask(null);

        /// <summary>
        /// Get the render task for exporting the currently selected content to the given <paramref name="path"/>.
        /// If the path is null, ask the user for an export path.
        /// </summary>
        /// <returns>A task for rendering or null when an error happened.</returns>
        Action GetRenderTask(string path) {
            try {
                PreRender();
            } catch (Exception e) {
                Error(e.Message);
                return null;
            }

            CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
            if (!reportMode.IsChecked) {
                if (path == null) {
                    SaveFileDialog dialog = new() {
                        FileName = fileName.Text.Contains('.') ? fileName.Text[..fileName.Text.LastIndexOf('.')] : fileName.Text
                    };
                    if (Directory.Exists(Settings.Default.lastDirectory)) {
                        dialog.InitialDirectory = Settings.Default.lastDirectory;
                    }

                    Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
                    dialog.Filter = MergeToContainer.GetPossibleContainers(target, codec, ffmpeg);
                    if (!dialog.ShowDialog().Value) {
                        return null;
                    }
                    path = dialog.FileName;
                }

                try {
                    return Render(path);
                } catch (Exception e) {
                    Error(e.Message);
                    return null;
                }
            } else {
                SetBlockSize(RenderTarget);
                try {
                    return () => RenderTask(target, null, null);
                } catch (Exception e) {
                    Error(e.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Setup write cache block size depending on active settings.
        /// </summary>
        void SetBlockSize(RenderTarget target) {
            int updateRate = environment.Listener.UpdateRate;
            blockSize = FiltersUsed ? roomCorrection[0].Length : defaultWriteCacheLength;
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
            LicenceWindow licenceWindow = Dispatcher.Invoke(() => new LicenceWindow());
            ExternalConverterHandler external = new(target, Consts.Language.GetExternalConverterStrings(), licenceWindow,
                taskEngine.UpdateProgressBar, taskEngine.UpdateStatus, Dispatcher.Invoke);
            external.Attach(environment.Listener, new DynamicUpmixingSettings(), keepFirstSources);
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
            taskEngine.UpdateStatus((string)language["Start"]);
            RenderTarget renderTargetRef = Dispatcher.Invoke(() => RenderTarget);
            RenderStats stats = WriteRender(target, writer, renderTargetRef);
            report.Generate(stats);

            string targetCodec = null;
            audio.Dispatcher.Invoke(() => targetCodec = ((ExportFormat)audio.SelectedItem).FFName);

            if (writer is RIFFWaveWriter && finalName[^4..] != waveExtension) {
                taskEngine.UpdateStatus("Merging to final container...");
                string exportedAudio = finalName[..^4] + waveExtension;
                MergeToContainer merger = new(file.Path, exportedAudio, targetCodec);
                merger.SetTrackName($"Cavern {renderTargetRef.Name} render");
                if (writer.ChannelCount > 8) {
                    merger.Allow8PlusChannels();
                }
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
            ExternalConverterHandler external = CreateExternalHandler(target, writer is DolbyAtmosBWFWriter ? 10 : 0);
            if (external.Failed) {
                return;
            }

            taskEngine.Progress = 0;
            taskEngine.UpdateStatus((string)language["Start"]);

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
            taskEngine.UpdateStatus((string)language["ExpOk"]);
            taskEngine.Progress = 1;

            if (Program.ConsoleMode) {
                Dispatcher.Invoke(Close);
            }

            if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.WorkedAround) {
                Error((string)language["JocWa"]);
            }
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
}