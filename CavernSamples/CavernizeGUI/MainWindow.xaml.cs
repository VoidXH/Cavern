using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Cavern;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;
using VoidX.WPF;

using Path = System.IO.Path;
using System.Diagnostics;

namespace CavernizeGUI {
    public partial class MainWindow : Window {
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

        // TODO: settable codec
        string codec = "libopus";

        /// <summary>
        /// Initialize the window and load last settings.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
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
        }

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.ffmpegLocation = ffmpeg.Location;
            Settings.Default.renderTarget = renderTarget.SelectedIndex;
            Settings.Default.Save();
            base.OnClosed(e);
        }

        /// <summary>
        /// Reset the listener and remove the objects of the last render.
        /// </summary>
        void Reset() {
            listener.DetachAllSources();
            if (file != null)
                file.Dispose();
            tracks.Visibility = Visibility.Hidden;
            tracks.ItemsSource = null;
            trackInfo.Text = string.Empty;
            report.Text = string.Empty;
        }

        /// <summary>
        /// Shows a popup about what channel should be wired to which output.
        /// </summary>
        void DisplayWiring(object _, RoutedEventArgs e) {
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).Channels;
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < prototypes.Length; ++i)
                output.AppendLine(string.Format((string)language["ChCon"], prototypes[i].Name,
                    ChannelPrototype.Get(i, prototypes.Length).Name));
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
                file = new(filePath = dialog.FileName);
                if (file.Tracks.Count != 0) {
                    tracks.Visibility = Visibility.Visible;
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

                // TODO: TEMPORARY, REMOVE WHEN AC-3 CAN BE DECODED
                string decode = dialog.FileName[..dialog.FileName.LastIndexOf('.')] + ".wav";
                if (File.Exists(decode)) {
                    AudioReader reader = AudioReader.Open(decode);
                    for (int i = 0; i < file.Tracks.Count; ++i)
                        file.Tracks[i].SetRendererSource(reader);
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
            for (int ch = 0; ch < channels.Length; ++ch)
                systemChannels[ch] =
                    new Channel(Renderer.channelPositions[(int)channels[ch]], channels[ch] == ReferenceChannel.ScreenLFE);
            Listener.ReplaceChannels(systemChannels);

            foreach (KeyValuePair<ReferenceChannel, Ellipse> pair in channelDisplay)
                pair.Value.Fill = red;
            for (int ch = 0; ch < channels.Length; ++ch)
                if (channelDisplay.ContainsKey(channels[ch]))
                    channelDisplay[channels[ch]].Fill = green;
        }

        /// <summary>
        /// Display track metadata on track selection.
        /// </summary>
        void OnTrackSelected(object _, SelectionChangedEventArgs e) {
            if (tracks.SelectedItem != null)
                trackInfo.Text = ((Track)tracks.SelectedItem).Details;
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
            listener.DetachAllSources();
            target.Attach(listener);

            AudioWriter writer = null;
            string exportName; // Rendered by Cavern
            string finalName = null; // Packed by FFmpeg
            if (renderToFile.IsChecked.Value) {
                SaveFileDialog dialog = new() {
                    Filter = (string)language["ExFmt"]
                };
                if (dialog.ShowDialog().Value) {
                    finalName = dialog.FileName;
                    exportName = finalName[^4..].ToLower().Equals(".mkv") ? finalName[..^4] + "{0}.wav" : finalName;
                    exportName = exportName.Replace("'", ""); // This character cannot be escaped in concat TXTs
                    writer = new SegmentedAudioWriter(exportName, Listener.Channels.Length,
                        target.Length, target.SampleRate * 30 * 60, target.SampleRate, BitDepth.Int16);
                    if (writer == null) {
                        Error((string)language["UnExt"]);
                        return;
                    }
                    writer.WriteHeader();
                } else // Cancel
                    return;
            }

            bool dynamic = dynamicOnly.IsChecked.Value;
            bool height = heightOnly.IsChecked.Value;
            taskEngine.Run(() => RenderTask(target, writer, dynamic, height, finalName));
        }

        /// <summary>
        /// Perform rendering.
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
                    if (!ffmpeg.Launch(string.Format("-i \"{0}\" -map 0:a:{1} -f segment -segment_time 30:00 \"{2}\"",
                        filePath, target.Index, string.Format(tempWAV, "%d"))) ||
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

            taskEngine.UpdateStatus("Starting render...");
            RenderStats stats = Exporting.WriteRender(listener, target, writer, taskEngine, dynamicOnly, heightOnly);
            UpdatePostRenderReport(stats);

            string targetCodec = codec;
            if (Listener.Channels.Length > 8)
                targetCodec += massivelyMultichannel;

            if (writer != null) {
                #region TODO: same
                string[] toConcat = ((SegmentedAudioWriter)writer).GetSegmentFiles();
                for (int i = 0; i < toConcat.Length; ++i)
                    toConcat[i] = $"file \'{toConcat[i]}\'";
                string concatList = finalName[..^4] + ".txt";
                string concatTarget = finalName[..^4] + "_tmp.mkv";
                File.WriteAllLines(concatList, toConcat);
                taskEngine.UpdateStatus("Encoding render...");
                if (!ffmpeg.Launch($"-f concat -safe 0 -i \"{concatList}\" -c {targetCodec} \"{concatTarget}\"")) {
                    taskEngine.UpdateStatus("Failed to create the encoded render. " +
                        "Are your permissions sufficient in the export folder?");
                    return;
                }
                #endregion

                if (finalName[^4..].ToLower().Equals(".mkv")) {
                    taskEngine.UpdateStatus("Merging to final container...");
                    string layout = null;
                    Dispatcher.Invoke(() => layout = ((RenderTarget)renderTarget.SelectedItem).Name);
                    if (!ffmpeg.Launch(string.Format("-i \"{0}\" -i \"{1}\" -map 0:v? -map 1:a -c copy -y " +
                        "-metadata:s:a:0 title=\"Cavern {2} render\" \"{3}\"",
                        filePath, concatTarget, layout, finalName)) ||
                        !File.Exists(finalName)) {
                        taskEngine.UpdateStatus("Failed to create the final file. " +
                            "Are your permissions sufficient in the export folder?");
                        return;
                    }
                    writer.Dispose();
                    toConcat = ((SegmentedAudioWriter)writer).GetSegmentFiles();
                    for (int i = 0; i < toConcat.Length; ++i)
                        File.Delete(toConcat[i]);
                    File.Delete(concatList);
                    File.Delete(concatTarget);
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