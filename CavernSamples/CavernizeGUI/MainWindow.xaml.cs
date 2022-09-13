using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Cavern;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Environment;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;
using VoidX.WPF;

using Path = System.IO.Path;

namespace CavernizeGUI {
    public partial class MainWindow : Window {
        /// <summary>
        /// Latest stable software version of Cavernize GUI.
        /// </summary>
        const string version = "1.5";

        /// <summary>
        /// This option allows FFmpeg to encode up to 255 channels in select codecs.
        /// </summary>
        const string massivelyMultichannel = " -mapping_family 255";

        /// <summary>
        /// The matching displayed dot for each supported channel.
        /// </summary>
        readonly Dictionary<ReferenceChannel, Ellipse> channelDisplay;

        /// <summary>
        /// FFmpeg runner and locator.
        /// </summary>
        readonly FFmpeg ffmpeg;

        /// <summary>
        /// Playback environment used for rendering.
        /// </summary>
        readonly Listener listener;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        readonly ResourceDictionary language = new ResourceDictionary();

        /// <summary>
        /// Runs the process in the background.
        /// </summary>
        readonly TaskEngine taskEngine;

        AudioFile file;

        string filePath;

        /// <summary>
        /// Initialize the window and load last settings.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            ad.Text = $"v{version} {ad.Text}";
            channelDisplay = new() {
                [ReferenceChannel.FrontLeft] = frontLeft,
                [ReferenceChannel.FrontCenter] = frontCenter,
                [ReferenceChannel.FrontRight] = frontRight,
                [ReferenceChannel.WideLeft] = wideLeft,
                [ReferenceChannel.WideRight] = wideRight,
                [ReferenceChannel.SideLeft] = sideLeft,
                [ReferenceChannel.SideRight] = sideRight,
                [ReferenceChannel.RearLeft] = rearLeft,
                [ReferenceChannel.RearRight] = rearRight,
                [ReferenceChannel.TopFrontLeft] = topFrontLeft,
                [ReferenceChannel.TopFrontCenter] = topFrontCenter,
                [ReferenceChannel.TopFrontRight] = topFrontRight,
                [ReferenceChannel.TopSideLeft] = topSideLeft,
                [ReferenceChannel.TopSideRight] = topSideRight,
                [ReferenceChannel.GodsVoice] = godsVoice
            };

            audio.ItemsSource = new ExportFormat[] {
                new ExportFormat(Codec.Opus, "libopus", "Opus (transparent, small size)"),
                new ExportFormat(Codec.PCM_LE, "pcm_s16le", "PCM integer (lossless, large size)"),
                new ExportFormat(Codec.PCM_Float, "pcm_f32le", "PCM float (needless, largest size)"),
                new ExportFormat(Codec.ADM_BWF, string.Empty, "ADM Broadcast Wave Format (studio)")
            };
            audio.SelectedIndex = Settings.Default.outputCodec;

            ffmpeg = new(render, status, Settings.Default.ffmpegLocation);
            listener = new() { // Create a listener, which triggers the loading of saved environment settings
                UpdateRate = 64,
                AudioQuality = QualityModes.Perfect,
                LFESeparation = true
            };

            language.Source = new Uri(";component/Resources/Strings.xaml", UriKind.RelativeOrAbsolute);
            renderTarget.ItemsSource = RenderTarget.Targets;
            renderTarget.SelectedIndex = Math.Min(Math.Max(0, Settings.Default.renderTarget), RenderTarget.Targets.Length - 1);
            taskEngine = new(progress, status);
            Reset();
        }

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.ffmpegLocation = ffmpeg.Location;
            Settings.Default.renderTarget = renderTarget.SelectedIndex;
            Settings.Default.outputCodec = audio.SelectedIndex;
            Settings.Default.Save();
            base.OnClosed(e);
        }

        /// <summary>
        /// Reset the listener and remove the objects of the last render.
        /// </summary>
        void Reset() {
            listener.DetachAllSources();
            if (file != null) {
                file.Dispose();
                file = null;
            }
            fileName.Text = string.Empty;
            trackControls.Visibility = Visibility.Hidden;
            tracks.ItemsSource = null;
            trackInfo.Text = string.Empty;
            report.Text = (string)language["Reprt"];
        }

        /// <summary>
        /// Shows a popup about what channel should be wired to which output.
        /// </summary>
        void DisplayWiring(object _, RoutedEventArgs e) {
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).Channels;
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < prototypes.Length; ++i) {
                output.AppendLine(string.Format((string)language["ChCon"], prototypes[i].Name,
                    ChannelPrototype.Get(i, prototypes.Length).Name));
            }
            MessageBox.Show(output.ToString(), (string)language["WrGui"]);
        }

        /// <summary>
        /// Open file button event; loads a WAV file to <see cref="reader"/>.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["OpRun"]);
                return;
            }

            OpenFileDialog dialog = new() {
                Filter = (string)language["ImFmt"]
            };
            if (dialog.ShowDialog().Value) {
                Reset();
                ffmpeg.CheckFFmpeg();
                taskEngine.UpdateProgressBar(0);

                fileName.Text = Path.GetFileName(dialog.FileName);

                try {
                    file = new(filePath = dialog.FileName);
                } catch (Exception ex) {
                    Reset();
                    if (ex is IOException) {
                        Error(ex.Message);
                    } else {
                        Error($"{ex.Message} {(string)language["Later"]}");
                    }
                    return;
                }

                if (file.Tracks.Count != 0) {
                    trackControls.Visibility = Visibility.Visible;
                    tracks.ItemsSource = file.Tracks;
                    tracks.SelectedIndex = 0;
                    // Prioritize spatial codecs
                    for (int i = 0, c = file.Tracks.Count; i < c; ++i) {
                        if (file.Tracks[i].Codec == Codec.EnhancedAC3) {
                            tracks.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Display the selected render target's active channels.
        /// </summary>
        void OnRenderTargetSelected(object _, SelectionChangedEventArgs e) {
            SolidColorBrush green = new SolidColorBrush(Colors.Green),
                red = new SolidColorBrush(Colors.Red);
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).Channels;

            Channel[] systemChannels = new Channel[channels.Length];
            for (int ch = 0; ch < channels.Length; ++ch) {
                systemChannels[ch] =
                    new Channel(Renderer.channelPositions[(int)channels[ch]], channels[ch] == ReferenceChannel.ScreenLFE);
            }
            Listener.ReplaceChannels(systemChannels);

            foreach (KeyValuePair<ReferenceChannel, Ellipse> pair in channelDisplay) {
                pair.Value.Fill = red;
            }
            for (int ch = 0; ch < channels.Length; ++ch) {
                if (channelDisplay.ContainsKey(channels[ch])) {
                    channelDisplay[channels[ch]].Fill = green;
                }
            }
        }

        /// <summary>
        /// Display track metadata on track selection.
        /// </summary>
        void OnTrackSelected(object _, SelectionChangedEventArgs e) {
            if (tracks.SelectedItem != null) {
                trackInfo.Text = ((Track)tracks.SelectedItem).Details;
            }
        }

        /// <summary>
        /// Prompt the user to select FFmpeg's installation folder.
        /// </summary>
        void LocateFFmpeg(object _, RoutedEventArgs e) {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["OpRun"]);
                return;
            }

            ffmpeg.Locate();
        }

        /// <summary>
        /// Start the rendering process.
        /// </summary>
        void Render(object _, RoutedEventArgs e) {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["OpRun"]);
                return;
            }
            if (tracks.SelectedItem == null) {
                Error((string)language["LdSrc"]);
                return;
            }
            Track target = (Track)tracks.SelectedItem;
            if (!target.Supported) {
                Error((string)language["UnTrk"]);
                return;
            }
            listener.SampleRate = target.SampleRate;
            listener.DetachAllSources();
            target.Attach(listener);

            if (target.Codec == Codec.EnhancedAC3) {
                listener.Volume = .5f; // Master volume of most E-AC-3 files is -6 dB, not yet applied from the stream
                listener.LFEVolume = 2;
            }

            AudioWriter writer = null;
            EnvironmentWriter transcoder = null;
            string exportName; // Rendered by Cavern
            string finalName = null; // Packed by FFmpeg
            bool isBWF = ((ExportFormat)audio.SelectedItem).Codec == Codec.ADM_BWF;
            if (renderToFile.IsChecked.Value) {
                SaveFileDialog dialog = new() {
                    Filter = !isBWF ? (string)language["ExFmt"] : (string)language["ExBWF"]
                };
                if (dialog.ShowDialog().Value) {
                    if (!isBWF) {
                        finalName = dialog.FileName;
                        exportName = finalName[^4..].ToLower().Equals(".mkv") ? finalName[..^4] + ".wav" : finalName;
                        writer = new RIFFWaveWriter(exportName, Listener.Channels.Length,
                            target.Length, target.SampleRate, BitDepth.Int16);
                        if (writer == null) {
                            Error((string)language["UnExt"]);
                            return;
                        }
                        writer.WriteHeader();
                    } else {
                        transcoder = new BroadcastWaveFormatWriter(dialog.FileName, listener, target.Length, BitDepth.Int24);
                    }
                } else { // Cancel
                    return;
                }
            }

            if (transcoder == null) {
                bool dynamic = dynamicOnly.IsChecked.Value;
                bool height = heightOnly.IsChecked.Value;
                taskEngine.Run(() => RenderTask(target, writer, dynamic, height, finalName));
            } else {
                taskEngine.Run(() => TranscodeTask(target, transcoder));
            }
        }

        /// <summary>
        /// Render the content and export it to a channel-based format.
        /// </summary>
        void RenderTask(Track target, AudioWriter writer, bool dynamicOnly, bool heightOnly, string finalName) {
            taskEngine.UpdateProgressBar(0);
#region TODO: TEMPORARY, REMOVE WHEN AC-3 AND MKV CAN BE FULLY DECODED - decode with FFmpeg until then
            bool isWAV = filePath[^4..].Equals(".wav");
            string tempWAV = filePath[..^4] + "{0}.wav";
            string firstWAV = string.Format(tempWAV, "0");
            SegmentedAudioReader wavReader = null;

            if (writer != null && !isWAV) {
                if (!File.Exists(firstWAV)) {
                    taskEngine.UpdateStatus("Decoding bed audio...");
                    if (!ffmpeg.Launch(string.Format("-drc_scale 0 -i \"{0}\" -map 0:a:{1} -c:a pcm_s24le " +
                        "-f segment -segment_time 30:00 \"{2}\"",
                        filePath, file.TryForBetterQuality(target), string.Format(tempWAV, "%d"))) ||
                        !File.Exists(firstWAV)) {
                        if (File.Exists(firstWAV))
                            File.Delete(firstWAV); // Only the first determines if it should be rendered
                        taskEngine.UpdateStatus("Failed to decode bed audio. " +
                            "Are your permissions sufficient in the source's folder?");
                        return;
                    }
                }
                target.SetRendererSource(wavReader = new SegmentedAudioReader(tempWAV));
                target.SetupForExport();
            }
#endregion

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
                    string layout = null;
                    Dispatcher.Invoke(() => layout = ((RenderTarget)renderTarget.SelectedItem).Name);
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

#region TODO: same
                if (wavReader != null) {
                    wavReader.Dispose();
                    int index = 0;
                    do {
                        File.Delete(firstWAV);
                        firstWAV = string.Format(tempWAV, ++index);
                    } while (File.Exists(firstWAV));
                }
#endregion
            }

            taskEngine.UpdateStatus((string)language["ExpOk"]);
            taskEngine.UpdateProgressBar(1);
        }

        /// <summary>
        /// Decode the source and export it to an object-based format.
        /// </summary>
        void TranscodeTask(Track target, EnvironmentWriter writer) {
            taskEngine.UpdateProgressBar(0);

#region TODO: TEMPORARY, REMOVE WHEN AC-3 AND MKV CAN BE FULLY DECODED - decode with FFmpeg until then
            SegmentedAudioReader wavReader = null;
            string tempWAV = filePath[..^4] + "{0}.wav";
            string firstWAV = string.Format(tempWAV, "0");
            if (target.Codec == Codec.AC3 || target.Codec == Codec.EnhancedAC3) {
                if (!File.Exists(firstWAV)) {
                    taskEngine.UpdateStatus("Decoding bed audio...");
                    if (!ffmpeg.Launch(string.Format("-drc_scale 0 -i \"{0}\" -map 0:a:{1} -c:a pcm_s24le " +
                        "-f segment -segment_time 30:00 \"{2}\"",
                        filePath, file.TryForBetterQuality(target), string.Format(tempWAV, "%d"))) ||
                        !File.Exists(firstWAV)) {
                        if (File.Exists(firstWAV))
                            File.Delete(firstWAV); // only the first determines if it should be rendered
                        taskEngine.UpdateStatus("Failed to decode bed audio. " +
                            "Are your permissions sufficient in the source's folder?");
                        return;
                    }
                }
                target.SetRendererSource(wavReader = new SegmentedAudioReader(tempWAV));
                target.SetupForExport();
            }
#endregion

            taskEngine.UpdateStatus((string)language["Start"]);
            RenderStats stats = Exporting.WriteTranscode(listener, target, writer, taskEngine);
            UpdatePostRenderReport(stats);
            taskEngine.UpdateStatus((string)language["ExpOk"]);
            taskEngine.UpdateProgressBar(1);

#region TODO: same
            if (wavReader != null) {
                wavReader.Dispose();
                int index = 0;
                do {
                    File.Delete(firstWAV);
                    firstWAV = string.Format(tempWAV, ++index);
                } while (File.Exists(firstWAV));
            }
#endregion
        }

        /// <summary>
        /// Opens the software's documentation.
        /// </summary>
        void Guide(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "http://cavern.sbence.hu/cavern/doc.php?p=CavernizeGUI",
            UseShellExecute = true
        });

        /// <summary>
        /// Open Cavern's website.
        /// </summary>
        void Ad(object _, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "http://cavern.sbence.hu",
            UseShellExecute = true
        });

        /// <summary>
        /// Displays an error message.
        /// </summary>
        void Error(string error) =>
            MessageBox.Show(error, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
    }
}