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
using Cavern.Remapping;
using Cavern.Utilities;
using VoidX.WPF;

using Path = System.IO.Path;

namespace CavernizeGUI {
    public partial class MainWindow : Window {
        /// <summary>
        /// Tells if a rendering process is in progress.
        /// </summary>
        public bool Rendering => taskEngine.IsOperationRunning;

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

            audio.ItemsSource = ExportFormat.Formats;
            audio.SelectedIndex = Settings.Default.outputCodec;

            ffmpeg = new(render, status, Settings.Default.ffmpegLocation);
            listener = new() { // Create a listener, which triggers the loading of saved environment settings
                UpdateRate = 64,
                AudioQuality = QualityModes.Perfect,
                LFESeparation = true
            };
            Listener.HeadphoneVirtualizer = false;

            language.Source = new Uri(";component/Resources/Strings.xaml", UriKind.RelativeOrAbsolute);
            renderTarget.ItemsSource = RenderTarget.Targets;
            renderTarget.SelectedIndex = Math.Min(Math.Max(0, Settings.Default.renderTarget), RenderTarget.Targets.Length - 1);
            renderSettings.IsEnabled = true; // Don't grey out initially
            taskEngine = new(progress, status);
            Reset();
        }

        /// <summary>
        /// Loads a content file into the application for processing.
        /// </summary>
        public void OpenContent(string path) {
            Reset();
            ffmpeg.CheckFFmpeg();
            taskEngine.UpdateProgressBar(0);

            fileName.Text = Path.GetFileName(path);
            OnOutputSelected(null, null);

            try {
                file = new(filePath = path);
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

        /// <summary>
        /// Start rendering to a target file.
        /// </summary>
        public void RenderContent(string path) {
            if (PreRender()) {
                Render(path);
            }
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
                OpenContent(dialog.FileName);
            }
        }

        /// <summary>
        /// Display the selected render target's active channels.
        /// </summary>
        void OnRenderTargetSelected(object _, SelectionChangedEventArgs e) {
            SolidColorBrush green = new SolidColorBrush(Colors.Green),
                red = new SolidColorBrush(Colors.Red);
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).Channels;
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
        /// Grey out renderer settings when it's not applicable.
        /// </summary>
        void OnOutputSelected(object _, SelectionChangedEventArgs e) =>
            renderSettings.IsEnabled = ((ExportFormat)audio.SelectedItem).Codec != Codec.ADM_BWF;

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
        /// Prepare the renderer for export.
        /// </summary>
        /// <returns>All checks succeeded and rendering can start.</returns>
        bool PreRender() {
            if (taskEngine.IsOperationRunning) {
                Error((string)language["OpRun"]);
                return false;
            }
            if (tracks.SelectedItem == null) {
                Error((string)language["LdSrc"]);
                return false;
            }

            Track target = (Track)tracks.SelectedItem;
            if (!target.Supported) {
                Error((string)language["UnTrk"]);
                return false;
            }

            ((RenderTarget)renderTarget.SelectedItem).Apply();
            if (((ExportFormat)audio.SelectedItem).MaxChannels < Listener.Channels.Length) {
                Error((string)language["ChCnt"]);
                return false;
            }

            listener.SampleRate = target.SampleRate;
            listener.DetachAllSources();
            target.Attach(listener);
            if (target.Codec == Codec.EnhancedAC3) {
                listener.Volume = .5f; // Master volume of most E-AC-3 files is -6 dB, not yet applied from the stream
                listener.LFEVolume = 2;
            }

            return true;
        }

        /// <summary>
        /// Start rendering to a target <paramref name="path"/>.
        /// </summary>
        void Render(string path) {
            Track target = (Track)tracks.SelectedItem;
            if (((ExportFormat)audio.SelectedItem).Codec != Codec.ADM_BWF) {
                ((RenderTarget)renderTarget.SelectedItem).Apply();
                string exportName = path[^4..].ToLower().Equals(".mkv") ? path[..^4] + ".wav" : path;
                AudioWriter writer = AudioWriter.Create(exportName, Listener.Channels.Length,
                    target.Length, target.SampleRate, BitDepth.Int16);
                if (writer == null) {
                    Error((string)language["UnExt"]);
                    return;
                }
                writer.WriteHeader();
                bool dynamic = dynamicOnly.IsChecked.Value;
                bool height = heightOnly.IsChecked.Value;
                taskEngine.Run(() => RenderTask(target, writer, dynamic, height, path));
            } else {
                EnvironmentWriter transcoder =
                    new BroadcastWaveFormatWriter(path, listener, target.Length, BitDepth.Int24);
                taskEngine.Run(() => TranscodeTask(target, transcoder));
            }
        }

        /// <summary>
        /// Start the rendering process.
        /// </summary>
        void Render(object _, RoutedEventArgs e) {
            if (!PreRender()) {
                return;
            }

            if (renderToFile.IsChecked.Value) {
                SaveFileDialog dialog = new() {
                    Filter = ((ExportFormat)audio.SelectedItem).Codec != Codec.ADM_BWF ?
                        (string)language["ExFmt"] : (string)language["ExBWF"]
                };
                if (dialog.ShowDialog().Value) {
                    Render(dialog.FileName);
                }
            } else {
                Track target = (Track)tracks.SelectedItem;
                taskEngine.Run(() => RenderTask(target, null, false, false, null));
            }
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

        /// <summary>
        /// Latest stable software version of Cavernize GUI.
        /// </summary>
        const string version = "1.5";

        /// <summary>
        /// This option allows FFmpeg to encode up to 255 channels in select codecs.
        /// </summary>
        const string massivelyMultichannel = " -mapping_family 255";
    }
}