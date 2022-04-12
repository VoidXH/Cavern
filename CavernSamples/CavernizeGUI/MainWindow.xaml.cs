using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Numerics;
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
                file = new(dialog.FileName);
                tracks.ItemsSource = file.Tracks;
                if (file.Tracks.Count != 0)
                    tracks.SelectedIndex = 0;

                // TODO: TEMPORARY, REMOVE WHEN AC-3 CAN BE DECODED
                string decode = dialog.FileName[..dialog.FileName.LastIndexOf('.')] + ".wav";
                if (System.IO.File.Exists(decode)) {
                    RIFFWaveReader reader = new RIFFWaveReader(decode);
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
            if (renderToFile.IsChecked.Value) {
                SaveFileDialog dialog = new() {
                    Filter = (string)language["ExFmt"]
                };
                if (dialog.ShowDialog().Value) {
                    writer = AudioWriter.Create(dialog.FileName, Listener.Channels.Length, target.Length, target.SampleRate,
                        BitDepth.Int16); // TODO: ability to change
                    if (writer == null) {
                        Error((string)language["UnExt"]);
                        return;
                    }
                    writer.WriteHeader();
                } else // Cancel
                    return;
            }

            taskEngine.Run(() => RenderTask(target, writer));
        }

        /// <summary>
        /// The running render process.
        /// </summary>
        void RenderTask(Track target, AudioWriter writer) {
            taskEngine.UpdateStatus("Starting render...");
            taskEngine.UpdateProgressBar(0);
            RenderStats stats = new(listener);
            const long updateInterval = 50000;
            long rendered = 0,
                untilUpdate = updateInterval;
            double samplesToProgress = 1.0 / target.Length,
                samplesToSeconds = 1.0 / listener.SampleRate;
            DateTime start = DateTime.Now;

            while (rendered < target.Length) {
                float[] result = listener.Render();
                if (target.Length - rendered < listener.UpdateRate)
                    Array.Resize(ref result, (int)(target.Length - rendered));
                if (writer != null)
                    writer.WriteBlock(result, 0, result.LongLength);
                stats.Update();

                rendered += listener.UpdateRate;

                if ((untilUpdate -= listener.UpdateRate) <= 0) {
                    double progress = rendered * samplesToProgress;
                    double speed = rendered * samplesToSeconds / (DateTime.Now - start).TotalSeconds;
                    taskEngine.UpdateStatusLazy($"Rendering... ({progress:0.00%}, speed: {speed:0.00}x)");
                    taskEngine.UpdateProgressBar(progress);
                    untilUpdate = updateInterval;
                }
            }

            if (writer != null)
                writer.Dispose();
            UpdatePostRenderReport(stats);
            taskEngine.UpdateStatus("Finished!");
            taskEngine.UpdateProgressBar(1);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="error"></param>
        static void Error(string error) => MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}