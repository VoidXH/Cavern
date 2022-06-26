using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Cavern;
using Cavern.Format;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;
using VoidX.WPF;

using Path = System.IO.Path;

namespace CavernizeGUI {
    public partial class MainWindow : Window {
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
            layout.Text = string.Format(string.Format((string)language["CavLo"], Listener.GetLayoutName()));
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
            tracks.ItemsSource = null;
            trackInfo.Text = string.Empty;
            report.Text = string.Empty;
        }

        /// <summary>
        /// Open file button event; loads a WAV file to <see cref="reader"/>.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = (string)language["ImFmt"]
            };
            if (dialog.ShowDialog().Value) {
                Reset();
                fileName.Text = Path.GetFileName(dialog.FileName);
                file = new(filePath = dialog.FileName);
                tracks.ItemsSource = file.Tracks;
                if (file.Tracks.Count != 0)
                    tracks.SelectedIndex = 0;

                // TODO: TEMPORARY, REMOVE WHEN AC-3 CAN BE DECODED
                string decode = dialog.FileName[..dialog.FileName.LastIndexOf('.')] + ".wav";
                if (System.IO.File.Exists(decode)) {
                    AudioReader reader = AudioReader.Open(decode);
                    for (int i = 0; i < file.Tracks.Count; ++i)
                        file.Tracks[i].SetRendererSource(reader);
                }
            }
        }

        /// <summary>
        /// Closes the main window, thus the application.
        /// </summary>
        void CloseWindow(object _, RoutedEventArgs e) => Close();

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
        void LocateFFmpeg(object _, RoutedEventArgs e) => ffmpeg.Locate();

        /// <summary>
        /// Start the rendering process.
        /// </summary>
        void Render(object _, RoutedEventArgs e) {
            if (tracks.SelectedItem == null) {
                Error((string)language["LdSrc"]);
                return;
            }
            Track target = (Track)tracks.SelectedItem;
            target.SetupForExport();
            if (!target.Supported) {
                Error((string)language["UnTrk"]);
                return;
            }
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
                    exportName = finalName[^4..].ToLower().Equals(".mkv") ? finalName[..^4] + ".wav" : finalName;
                    writer = AudioWriter.Create(exportName, Listener.Channels.Length, target.Length, target.SampleRate,
                        BitDepth.Int16);
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
            // TODO: TEMPORARY, REMOVE WHEN AC-3 CAN BE DECODED - decode with FFmpeg
            string tempName = filePath[..^4] + ".wav";
            if (!File.Exists(tempName)) {
                taskEngine.UpdateStatus("Decoding bed audio...");
                if (ffmpeg.Launch(string.Format("-i \"{0}\" \"{1}\"", filePath, tempName)) &&
                    File.Exists(tempName)) {
                    target.SetRendererSource(AudioReader.Open(tempName));
                    target.SetupForExport();
                } else {
                    if (File.Exists(tempName))
                        File.Delete(tempName);
                    taskEngine.UpdateStatus("Failed to decode bed audio. " +
                        "Are your permissions sufficient in the source's folder?");
                    return;
                }
            }

            taskEngine.UpdateStatus("Starting render...");
            RenderStats stats = Exporting.WriteRender(listener, target, writer, taskEngine, dynamicOnly, heightOnly);
            UpdatePostRenderReport(stats);

            if (finalName[^4..].ToLower().Equals(".mkv")) {
                taskEngine.UpdateStatus("Converting to final format...");
                string exportName = finalName[..^4] + ".wav";
                if (!ffmpeg.Launch(string.Format("-i \"{0}\" -i \"{1}\" -map 0:v? -map 1:a -c:v copy -c:a {2} -y \"{3}\"",
                    filePath, exportName, codec, finalName)) ||
                    !File.Exists(finalName)) {
                    taskEngine.UpdateStatus("Failed to create the final file. " +
                        "Are your permissions sufficient in the export folder?");
                    return;
                }
                File.Delete(exportName);
            }

            taskEngine.UpdateStatus("Finished!");
            taskEngine.UpdateProgressBar(1);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string error) => MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}