using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

using Cavern.Format;
using Cavern.Remapping;

using CavernizeGUI.Elements;
using CavernizeGUI.Windows;

namespace CavernizeGUI {
    public partial class MainWindow {
        /// <summary>
        /// Sample rate of the <see cref="roomCorrection"/> filters.
        /// </summary>
        int roomCorrectionSampleRate;

        /// <summary>
        /// Convolution filter for each channel to be applied on export.
        /// </summary>
        float[][] roomCorrection;

        /// <summary>
        /// Opens the upmixing settings.
        /// </summary>
        void OpenUpmixSetup(object _, RoutedEventArgs e) {
            UpmixingSetup setup = new UpmixingSetup {
                Title = (string)language["UpmiW"]
            };
            setup.ShowDialog();
        }

        /// <summary>
        /// Load a set of room correction filters to EQ the exported content.
        /// </summary>
        void LoadFilters(object _, RoutedEventArgs e) {
            if (!filters.IsChecked) {
                OpenFileDialog dialog = new() {
                    Filter = (string)language["FiltF"]
                };
                if (dialog.ShowDialog().Value) {
                    int cutoff = dialog.FileName.IndexOf('.');
                    if (cutoff == -1) {
                        return;
                    }
                    string pathStart = dialog.FileName[..cutoff] + ' ';

                    ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).GetNameMappedChannels();
                    roomCorrection = new float[channels.Length][];
                    for (int i = 0; i < channels.Length; i++) {
                        string file = $"{pathStart}{channels[i].GetShortName()}.wav";
                        if (File.Exists(file)) {
                            using RIFFWaveReader reader = new RIFFWaveReader(file);
                            roomCorrection[i] = reader.Read();
                            roomCorrectionSampleRate = reader.SampleRate;
                        } else {
                            Error(string.Format((string)language["FiltN"], ChannelPrototype.Mapping[(int)channels[i]].Name,
                                Path.GetFileName(dialog.FileName)));
                            roomCorrection = null;
                            return;
                        }
                    }
                    filters.IsChecked = true;
                }
            } else {
                filters.IsChecked = false;
                roomCorrection = null;
            }
        }

        /// <summary>
        /// Show the post-render report in a popup.
        /// </summary>
        void ShowPostRenderReport(object _, RoutedEventArgs e) => MessageBox.Show(report, (string)language["PReRe"]);

        /// <summary>
        /// Shows a popup about what channel should be wired to which output.
        /// </summary>
        void DisplayWiring(object _, RoutedEventArgs e) {
            ReferenceChannel[] channels = ((RenderTarget)renderTarget.SelectedItem).GetNameMappedChannels();
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < prototypes.Length; ++i) {
                output.AppendLine(string.Format((string)language["ChCon"], prototypes[i].Name,
                    ChannelPrototype.Get(i, prototypes.Length).Name));
            }
            MessageBox.Show(output.ToString(), (string)language["WrGui"]);
        }
    }
}