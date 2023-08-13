using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using Cavern.Channels;
using Cavern.Format;
using VoidX.WPF;

namespace WAVChannelReorderer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Supported file formats for both import and export.
        /// </summary>
        const string dialogFilter = "RIFF WAVE files (*.wav)|*.wav";

        /// <summary>
        /// Imported audio file handle.
        /// </summary>
        RIFFWaveReader reader;

        /// <summary>
        /// Path to the imported audio file.
        /// </summary>
        string loadedFile;

        /// <summary>
        /// Dune HD only supports 24-bit 5.1 WAV files.
        /// </summary>
        bool force24Bits;

        /// <summary>
        /// Background export runner.
        /// </summary>
        readonly TaskEngine process;

        /// <summary>
        /// Initialize the WAV Channel Reorderer.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            process = new(progress, null, status);
        }

        /// <summary>
        /// Display an error message.
        /// </summary>
        static void ShowError(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Free resources when the application is closed.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            process?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// Loads a standard layout of the channel count of the imported file to a list of channels.
        /// </summary>
        void SetStandardLayout(ListBox holder) {
            holder.ItemsSource = (ReferenceChannel[])ChannelPrototype.GetStandardMatrix(reader.ChannelCount).Clone();
            force24Bits = false;
        }

        /// <summary>
        /// Sets up a standard layout for export.
        /// </summary>
        void StandardTarget(object sender, RoutedEventArgs e) => SetStandardLayout(targetChannels);

        /// <summary>
        /// Sets up a Dune HD player's layout for export.
        /// </summary>
        void DuneTarget(object sender, RoutedEventArgs e) {
            if (reader.ChannelCount < 6) {
                SetStandardLayout(targetChannels);
            } else if (reader.ChannelCount <= 8) {
                ReferenceChannel[] source = (ReferenceChannel[])duneLayouts[reader.ChannelCount - 6].Clone();
                targetChannels.ItemsSource = source;
                force24Bits = source.Length == 6;
            } else {
                ShowError("More than 8 raw channels are unsupported with any dedicated media player.");
            }
        }

        /// <summary>
        /// Import an audio file for processing.
        /// </summary>
        void OpenFile(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = dialogFilter
            };
            if (dialog.ShowDialog() == true) {
                try {
                    reader = new RIFFWaveReader(dialog.FileName);
                    reader.ReadHeader();
                } catch (Exception ex) {
                    ShowError(ex.Message);
                    return;
                }
                loadedFile = dialog.FileName;
                fileName.Text = Path.GetFileName(dialog.FileName);
                SetStandardLayout(sourceChannels);
                SetStandardLayout(targetChannels);
                export.IsEnabled = true;
            }
        }

        /// <summary>
        /// When selecting a channel on a channel mapping, allow its editing in the related combo box.
        /// </summary>
        static void OnChannelSelect(ChannelComboBox channel, ListBox channels) {
            channel.SelectedItem = channels.SelectedItem ?? null;
            channel.IsEnabled = true;
        }

        /// <summary>
        /// Enable and setup a source channel's editing.
        /// </summary>
        void SourceChannelSelected(object _, SelectionChangedEventArgs e) => OnChannelSelect(sourceChannel, sourceChannels);

        /// <summary>
        /// Enable and setup a target channel's editing.
        /// </summary>
        void TargetChannelSelected(object _, SelectionChangedEventArgs e) => OnChannelSelect(targetChannel, targetChannels);

        /// <summary>
        /// Update a channel mapping when a selected channel was swapped.
        /// </summary>
        static void OnChannelChange(ChannelComboBox channel, ListBox channels) {
            int oldIndex = channels.SelectedIndex;
            if (oldIndex < 0) {
                channel.IsEnabled = false;
                return;
            }
            ReferenceChannel[] list = (ReferenceChannel[])channels.ItemsSource;
            list[channels.SelectedIndex] = (ReferenceChannel)channel.SelectedItem;
            channels.ItemsSource = (ReferenceChannel[])list.Clone();
            channels.SelectedIndex = oldIndex;
        }

        /// <summary>
        /// Update input mapping when a source channel was swapped.
        /// </summary>
        void SourceChannelChanged(object _, SelectionChangedEventArgs e) => OnChannelChange(sourceChannel, sourceChannels);

        /// <summary>
        /// Update output mapping when a target channel was swapped.
        /// </summary>
        void TargetChannelChanged(object _, SelectionChangedEventArgs e) => OnChannelChange(targetChannel, targetChannels);

        /// <summary>
        /// Perform channel swapping while exporting to a file.
        /// </summary>
        void ExportProcess(string path) {
            int channels = reader.ChannelCount;
            int[] targetIndexes = new int[channels];
            for (int i = 0; i < channels; ++i) {
                targetIndexes[i] = -1;
                for (int j = 0; j < channels; ++j) {
                    if ((ReferenceChannel)sourceChannels.Items[i] == (ReferenceChannel)targetChannels.Items[j]) {
                        targetIndexes[i] = j;
                    }
                }
            }

            using RIFFWaveWriter writer = new(path, channels, reader.Length, reader.SampleRate, force24Bits ? BitDepth.Int24 : reader.Bits);
            reader.Reset();
            writer.WriteHeader();
            long position = 0,
                end = channels * reader.Length;
            float[] source = new float[channels * reader.SampleRate],
                target = new float[source.Length];

            while (position < end) {
                long stepSize = Math.Min(source.Length, end - position);
                reader.ReadBlock(source, 0, stepSize);
                Array.Clear(target, 0, (int)stepSize);
                for (int ch = 0; ch < channels; ++ch) {
                    int targetIndex = targetIndexes[ch];
                    if (targetIndex == -1) {
                        continue;
                    }
                    for (int sample = ch, pair = targetIndex; sample < source.Length; sample += channels, pair += channels) {
                        target[pair] = source[sample];
                    }
                }
                writer.WriteBlock(target, 0, stepSize);
                position += stepSize;

                double progress = position / (double)end;
                process.UpdateStatusLazy($"Exporting... ({progress:0.00%})");
                process.UpdateProgressBar(progress);
            }

            process.UpdateStatus("Finished!");
            process.Progress = 1;
        }

        /// <summary>
        /// Begin the export process with selecting the output name.
        /// </summary>
        void Export(object _, RoutedEventArgs e) {
            SaveFileDialog dialog = new() {
                Filter = dialogFilter
            };
            if (dialog.ShowDialog() == true) {
                if (dialog.FileName == loadedFile) {
                    ShowError("Can't overwrite the source file with the exported file.");
                } else {
                    process.Run(() => ExportProcess(dialog.FileName));
                }
            }
        }
    }
}