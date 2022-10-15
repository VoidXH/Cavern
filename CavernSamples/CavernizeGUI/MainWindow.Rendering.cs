using Microsoft.Win32;
using System;
using System.IO;

using Cavern;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Environment;
using Cavern.Utilities;

using CavernizeGUI.Elements;
using CavernizeGUI.Resources;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Prepare the renderer for export.
        /// </summary>
        void PreRender() {
            if (taskEngine.IsOperationRunning) {
                throw new Exception((string)language["OpRun"]);
            }
            if (tracks.SelectedItem == null) {
                throw new Exception((string)language["LdSrc"]);
            }

            if (!((Track)tracks.SelectedItem).Supported) {
                throw new Exception((string)language["UnTrk"]);
            }

            ((RenderTarget)renderTarget.SelectedItem).Apply();
            int maxChannels = ((ExportFormat)audio.SelectedItem).MaxChannels;
            if (maxChannels < Listener.Channels.Length) {
                throw new Exception(string.Format((string)language["ChCnt"], Listener.Channels.Length, maxChannels));
            }

            SoftPreRender(false);
        }

        /// <summary>
        /// Prepare the renderer for export, without safety checks.
        /// </summary>
        void SoftPreRender(bool applyTarget) {
            if (applyTarget) {
                ((RenderTarget)renderTarget.SelectedItem).Apply();
            }

            Track target = (Track)tracks.SelectedItem;
            listener.SampleRate = target.SampleRate;
            listener.DetachAllSources();
            target.Attach(listener);

            if (target.Codec == Codec.EnhancedAC3) {
                listener.Volume = .5f; // Master volume of most E-AC-3 files is -6 dB, not yet applied from the stream
                listener.LFEVolume = 2;
            } else {
                listener.Volume = 1;
                listener.LFEVolume = 1;
            }
        }

        /// <summary>
        /// Start rendering to a target <paramref name="path"/>.
        /// </summary>
        /// <returns>A task for rendering or null when an error happened.</returns>
        Action Render(string path) {
            Track target = (Track)tracks.SelectedItem;
            Codec codec = ((ExportFormat)audio.SelectedItem).Codec;
            if (!codec.IsEnvironmental()) {
                ((RenderTarget)renderTarget.SelectedItem).Apply();
                string exportName = path[^4..].ToLower().Equals(".mkv") ? path[..^4] + ".wav" : path;
                AudioWriter writer = AudioWriter.Create(exportName, Listener.Channels.Length,
                    target.Length, target.SampleRate, BitDepth.Int16);
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
                    case Codec.ADM_BWF:
                        transcoder = new BroadcastWaveFormatWriter(path, listener, target.Length, BitDepth.Int24);
                        break;
                    case Codec.ADM_BWF_Atmos:
                        transcoder = new DolbyAtmosBWFWriter(path, listener, target.Length, BitDepth.Int24);
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

            if (!reportMode.IsChecked) {
                SaveFileDialog dialog = new() {
                    Filter = ((ExportFormat)audio.SelectedItem).Codec.IsEnvironmental() ?
                        (string)language["ExBWF"] : (string)language["ExFmt"]
                };
                if (dialog.ShowDialog().Value) {
                    return Render(dialog.FileName);
                }
            } else {
                Track target = (Track)tracks.SelectedItem;
                return () => RenderTask(target, null, false, false, null);
            }
            return null;
        }

        /// <summary>
        /// Run all queued jobs one after another.
        /// </summary>
        void QueueRunnerTask(QueuedJob[] jobs) {
            Dispatcher.Invoke(() => queuedJobs.IsEnabled = false);
            for (int i = 0; i < jobs.Length; i++) {
                QueuedJob job = jobs[i];
                Dispatcher.Invoke(() => {
                    job.Prepare(this);
                    SoftPreRender(true);
                });
                job.Run();
                Dispatcher.Invoke(() => this.jobs.Remove(job));
            }
            Dispatcher.Invoke(() => queuedJobs.IsEnabled = true);
        }

        /// <summary>
        /// Render the content and export it to a channel-based format.
        /// </summary>
        void RenderTask(Track target, AudioWriter writer, bool dynamicOnly, bool heightOnly, string finalName) {
            taskEngine.UpdateProgressBar(0);
            taskEngine.UpdateStatus((string)language["Start"]);
            RenderStats stats = Exporting.WriteRender(listener, target, writer, taskEngine, dynamicOnly, heightOnly);
            UpdatePostRenderReport(stats);

            string targetCodec = null;
            audio.Dispatcher.Invoke(() => targetCodec = ((ExportFormat)audio.SelectedItem).FFName);

            if (writer != null) {
                if (writer.ChannelCount > 8) {
                    targetCodec += massivelyMultichannel;
                }

                if (finalName[^4..].ToLower().Equals(".mkv")) {
                    string exportedAudio = finalName[..^4] + ".wav";
                    taskEngine.UpdateStatus("Merging to final container...");
                    string layout = null,
                        filePath = null;
                    Dispatcher.Invoke(() => {
                        layout = ((RenderTarget)renderTarget.SelectedItem).Name;
                        filePath = file.Path;
                    });
                    if (!ffmpeg.Launch(string.Format("-i \"{0}\" -i \"{1}\" -map 0:v? -map 1:a -map 0:s? -c:v copy -c:a {2} " +
                        "-y -metadata:s:a:0 title=\"Cavern {3} render\" \"{4}\"",
                        filePath, exportedAudio, targetCodec, layout, finalName)) ||
                        !File.Exists(finalName)) {
                        taskEngine.UpdateStatus("Failed to create the final file. " +
                            "Are your permissions sufficient in the export folder?");
                        return;
                    }
                    File.Delete(exportedAudio);
                }
            }

            taskEngine.UpdateStatus((string)language["ExpOk"]);
            taskEngine.UpdateProgressBar(1);

            if (Program.ConsoleMode) {
                Dispatcher.Invoke(Close);
            }
        }

        /// <summary>
        /// Decode the source and export it to an object-based format.
        /// </summary>
        void TranscodeTask(Track target, EnvironmentWriter writer) {
            taskEngine.UpdateProgressBar(0);
            taskEngine.UpdateStatus((string)language["Start"]);
            RenderStats stats = Exporting.WriteTranscode(listener, target, writer, taskEngine);
            UpdatePostRenderReport(stats);
            taskEngine.UpdateStatus((string)language["ExpOk"]);
            taskEngine.UpdateProgressBar(1);
        }
    }
}