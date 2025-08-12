﻿using Microsoft.Win32;
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

using Cavernize.Logic;
using Cavernize.Logic.External;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;
using Cavernize.Logic.Rendering;
using CavernizeGUI.CavernSettings;
using CavernizeGUI.Elements;
using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// The voltage gain at which the content is rendered.
        /// </summary>
        public float renderGain = 1;

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

            RenderTarget selectedRenderTarget = (RenderTarget)renderTarget.SelectedItem;
            selectedRenderTarget.Apply(Settings.Default.surroundSwap);
            if (selectedRenderTarget is not VirtualizerRenderTarget && format.MaxChannels < Listener.Channels.Length) {
                throw new TrackException(string.Format((string)language["ChCnt"], Listener.Channels.Length, format.MaxChannels));
            }

            SoftPreRender(false);
        }

        /// <summary>
        /// Prepare the renderer for export, without safety checks.
        /// </summary>
        void SoftPreRender(bool applyTarget) {
            CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
            RenderTarget activeRenderTarget = (RenderTarget)renderTarget.SelectedItem;
            if (applyTarget) {
                activeRenderTarget.Apply(Settings.Default.surroundSwap);
            }
            if (activeRenderTarget is VirtualizerRenderTarget) {
                if (roomCorrection != null && roomCorrectionSampleRate != VirtualizerFilter.FilterSampleRate) {
                    throw new IncompatibleSettingsException((string)language["FiltC"]);
                }
                listener.SampleRate = VirtualizerFilter.FilterSampleRate;
            } else {
                listener.SampleRate = roomCorrection == null ? target.SampleRate : roomCorrectionSampleRate;
            }

            if (speakerVirtualizer.IsChecked) {
                try {
                    VirtualizerFilter.SetupForSpeakers();
                } catch {
                    throw new NonGroundChannelPresentException((string)language["SpViE"]);
                }
                listener.SampleRate = VirtualizerFilter.FilterSampleRate;
            }

            listener.DetachAllSources();
            target.Attach(listener, new DynamicUpmixingSettings());

            // Prevent height limiting, require at least 4 overhead channels for full gain
            listener.Volume = renderGain * (target.Renderer.HasObjects && Listener.Channels.GetOverheadChannelCount() < 4 ? .707f : 1);
        }

        /// <summary>
        /// Start rendering to a target <paramref name="path"/>.
        /// </summary>
        /// <returns>A task for rendering or null when an error happened.</returns>
        Action Render(string path) {
            RenderTarget activeRenderTarget = (RenderTarget)renderTarget.SelectedItem;
            CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
            Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
            BitDepth bits = codec == Codec.PCM_Float ? BitDepth.Float32 : force24Bit.IsChecked ? BitDepth.Int24 : BitDepth.Int16;
            if (!codec.IsEnvironmental()) {
                SetBlockSize(activeRenderTarget);
                string exportFormat = path[^4..].ToLowerInvariant();
                bool mkvTarget = exportFormat.Equals(".mkv");
                string exportName = mkvTarget || exportFormat.Equals(".ac3") || exportFormat.Equals(".ec3") ?
                    path[..^4] + waveExtension :
                    path;
                int channelCount = activeRenderTarget.OutputChannels;
                AudioWriter writer;
                if (mkvTarget && target.Container == Container.Matroska && (codec == Codec.PCM_LE || codec == Codec.PCM_Float)) {
                    writer = new AudioWriterIntoContainer(path, target.GetVideoTracks(), codec,
                        blockSize, channelCount, target.Length, target.SampleRate, bits) {
                        NewTrackName = $"Cavern {activeRenderTarget.Name} render"
                    };
                } else if (exportFormat.Equals(waveExtension) && !wavChannelSkip.IsChecked) {
                    writer = new RIFFWaveWriter(exportName, activeRenderTarget.Channels[..channelCount],
                        target.Length, listener.SampleRate, bits);
                } else {
                    writer = AudioWriter.Create(exportName, channelCount, target.Length, listener.SampleRate, bits);
                }
                if (writer == null) {
                    Error((string)language["UnExt"]);
                    return null;
                }
                writer.WriteHeader();
                bool dynamic = dynamicOnly.IsChecked;
                bool height = heightOnly.IsChecked;
                return () => RenderTask(target, writer, dynamic, height, path);
            } else {
                EnvironmentWriter transcoder;
                switch (codec) {
                    case Codec.LimitlessAudio:
                        transcoder = new LimitlessAudioFormatEnvironmentWriter(path, listener, target.Length, bits);
                        break;
                    case Codec.ADM_BWF:
                        transcoder = new BroadcastWaveFormatWriter(path, listener, target.Length, bits);
                        break;
                    case Codec.ADM_BWF_Atmos:
                        (ReferenceChannel, Source)[] staticObjects;
                        if (target.Renderer is EnhancedAC3Renderer eac3 && eac3.HasObjects) {
                            ReferenceChannel[] staticChannels = eac3.GetStaticChannels();
                            IReadOnlyList<Source> allObjects = eac3.Objects;
                            staticObjects = new (ReferenceChannel, Source)[staticChannels.Length];
                            for (int i = 0; i < staticChannels.Length; i++) {
                                staticObjects[i] = (staticChannels[i], allObjects[i]);
                            }
                        } else {
                            staticObjects = [];
                        }
                        transcoder = new DolbyAtmosBWFWriter(path, listener, target.Length, bits, staticObjects);
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
        Action GetRenderTask() {
            try {
                PreRender();
            } catch (Exception e) {
                Error(e.Message);
                return null;
            }

            CavernizeTrack target = (CavernizeTrack)tracks.SelectedItem;
            if (!reportMode.IsChecked) {
                SaveFileDialog dialog = new() {
                    FileName = fileName.Text.Contains('.') ? fileName.Text[..fileName.Text.LastIndexOf('.')] : fileName.Text
                };
                if (Directory.Exists(Settings.Default.lastDirectory)) {
                    dialog.InitialDirectory = Settings.Default.lastDirectory;
                }

                Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
                dialog.Filter = MergeToContainer.GetPossibleContainers(target, codec, ffmpeg);
                if (dialog.ShowDialog().Value) {
                    try {
                        return Render(dialog.FileName);
                    } catch (Exception e) {
                        Error(e.Message);
                        return null;
                    }
                }
            } else {
                SetBlockSize((RenderTarget)renderTarget.SelectedItem);
                try {
                    return () => RenderTask(target, null, false, false, null);
                } catch (Exception e) {
                    Error(e.Message);
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Setup write cache block size depending on active settings.
        /// </summary>
        void SetBlockSize(RenderTarget target) {
            blockSize = FiltersUsed ? roomCorrection[0].Length : defaultWriteCacheLength;
            if (blockSize < listener.UpdateRate) {
                blockSize = listener.UpdateRate;
            } else if (blockSize % listener.UpdateRate != 0) {
                // Cache handling is written to only handle when its size is divisible with the update rate - it's faster this way
                blockSize += listener.UpdateRate - blockSize % listener.UpdateRate;
            }
            blockSize *= target.OutputChannels;
        }

        /// <summary>
        /// Run all queued jobs one after another.
        /// </summary>
        void QueueRunnerTask(QueuedJob[] jobs) {
            Dispatcher.Invoke(() => queuedJobs.AllowDrop = false);
            for (int i = 0; i < jobs.Length; i++) {
                QueuedJob job = jobs[i];
                Dispatcher.Invoke(() => {
                    job.Prepare(this);
                    SoftPreRender(true);
                });
                job.Run();
                Dispatcher.Invoke(() => this.jobs.Remove(job));
            }
            Dispatcher.Invoke(() => queuedJobs.AllowDrop = true);
        }

        /// <summary>
        /// Render the content and export it to a channel-based format.
        /// </summary>
        void RenderTask(CavernizeTrack target, AudioWriter writer, bool dynamicOnly, bool heightOnly, string finalName) {
            RenderTarget renderTargetRef = null;
            Dispatcher.Invoke(() => renderTargetRef = (RenderTarget)renderTarget.SelectedItem);

            ExternalConverter external = null;
            if (target.Codec == Codec.TrueHD) { // Use truehdd if needed
                external = new Truehdd(Consts.Language.GetExternalConverterStrings());
            }
            if (external != null) {
                taskEngine.Progress = -1;
                external.UpdateStatus += taskEngine.UpdateStatus;
                Dispatcher.Invoke(() => {
                    external.LicenceDisplay = new LicenceWindow();
                    external.PrepareOnUI();
                });

                target = external.Convert(target);
                listener.DetachAllSources();
                target.Attach(listener, new DynamicUpmixingSettings());
            }

            taskEngine.Progress = 0;
            taskEngine.UpdateStatus((string)language["Start"]);
            RenderStats stats = WriteRender(target, writer, renderTargetRef, dynamicOnly, heightOnly);
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
                    external?.Cleanup();
                    return;
                }
            }

            external?.Cleanup();
            FinishTask(target);
        }

        /// <summary>
        /// Decode the source and export it to an object-based format.
        /// </summary>
        void TranscodeTask(CavernizeTrack target, EnvironmentWriter writer) {
            taskEngine.Progress = 0;
            taskEngine.UpdateStatus((string)language["Start"]);

            RenderStats stats;
            if (writer is BroadcastWaveFormatWriter bwf) {
                stats = WriteTranscode(target, bwf);
            } else {
                stats = WriteTranscode(target, writer);
            }
            report.Generate(stats);
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