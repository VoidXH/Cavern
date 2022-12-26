using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

using Cavern.Format;
using Cavern.Remapping;

using CavernizeGUI.Elements;
using CavernizeGUI.Windows;
using Cavern.Virtualizer;

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
        /// Load a set of HRTF impulses (HRIRs) for the Virtualizer.
        /// </summary>
        void LoadHRIR(object _, RoutedEventArgs e) {
            if (!hrir.IsChecked) {
                OpenFileDialog dialog = new() {
                    Filter = (string)language["FiltI"]
                };
                if (dialog.ShowDialog().Value) {
                    RIFFWaveReader? file = new RIFFWaveReader(dialog.FileName);
                    VirtualizerFilter.Override(VirtualChannel.Parse(file.ReadMultichannel(), file.SampleRate), file.SampleRate);
                    hrir.IsChecked = true;
                }
            } else {
                VirtualizerFilter.Reset();
                hrir.IsChecked = false;
            }
        }

        /// <summary>
        /// Load a set of room correction filters to EQ the exported content.
        /// </summary>
        void LoadFilters(object _, RoutedEventArgs e) {
            if (!filters.IsChecked) {
                OpenFileDialog dialog = new() {
                    Filter = (string)language["FiltF"]
                };
                int cutoff;
                if (!dialog.ShowDialog().Value || (cutoff = dialog.FileName.IndexOf('.')) == -1) {
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