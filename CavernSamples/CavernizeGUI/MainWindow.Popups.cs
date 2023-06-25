using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Cavern;
using Cavern.Channels;
using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Virtualizer;

using CavernizeGUI.Elements;
using CavernizeGUI.Windows;
using CavernizeGUI.Resources;

using Track = CavernizeGUI.Elements.Track;

namespace CavernizeGUI {
    public partial class MainWindow {
        /// <summary>
        /// Room correction is active and the loaded filter set for it is valid for the output system.
        /// </summary>
        bool FiltersUsed => roomCorrection != null && Listener.Channels.Length == roomCorrection.Channels;

        /// <summary>
        /// Sample rate of the <see cref="roomCorrection"/> filters.
        /// </summary>
        int roomCorrectionSampleRate;

        /// <summary>
        /// Convolution filter for each channel to be applied on export.
        /// </summary>
        MultichannelWaveform roomCorrection;

        /// <summary>
        /// Try to load a HRIR file and return true on success.
        /// If the file is invalid, an optional error message popup can be displayed.
        /// </summary>
        static bool TryLoadHRIR(bool popupOnError) {
            RIFFWaveReader file;
            try {
                file = new RIFFWaveReader(Settings.Default.hrirPath);
                file.ReadHeader();
            } catch (Exception e) {
                if (popupOnError) {
                    Error(string.Format((string)language["IrErr"], e.Message));
                }
                return false;
            }
            VirtualizerFilter.Override(VirtualChannel.Parse(new MultichannelWaveform(file.ReadMultichannelAfterHeader()), file.SampleRate),
                file.SampleRate);
            return true;
        }

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
                    Settings.Default.hrirPath = dialog.FileName;
                    hrir.IsChecked = TryLoadHRIR(true);
                }
            } else {
                VirtualizerFilter.Reset();
                Settings.Default.hrirPath = string.Empty;
                hrir.IsChecked = false;
            }
        }

        /// <summary>
        /// Load a set of room correction filters to EQ the exported content.
        /// </summary>
        void LoadFilters(object _, RoutedEventArgs __) {
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
                float[][] roomCorrectionSource = new float[channels.Length][];
                for (int i = 0; i < channels.Length; i++) {
                    string file = $"{pathStart}{channels[i].GetShortName()}.wav";
                    if (!File.Exists(file)) {
                        file = $"{pathStart}{i + 1}.wav";
                    }
                    if (File.Exists(file)) {
                        using RIFFWaveReader reader = new RIFFWaveReader(file);
                        roomCorrectionSource[i] = reader.Read();
                        roomCorrectionSampleRate = reader.SampleRate;
                    } else {
                        Error(string.Format((string)language["FiltN"], ChannelPrototype.Mapping[(int)channels[i]].Name,
                            Path.GetFileName(dialog.FileName)));
                        roomCorrection = null;
                        return;
                    }
                }
                filters.IsChecked = true;
                try {
                    roomCorrection = new MultichannelWaveform(roomCorrectionSource);
                } catch (Exception e) {
                    Error(e.Message);
                    filters.IsChecked = false;
                }
            } else {
                filters.IsChecked = false;
                roomCorrection = null;
            }
        }

        /// <summary>
        /// Show the post-render report in a popup.
        /// </summary>
        void ShowMetadata(object _, RoutedEventArgs e) {
            if (tracks.SelectedItem is not Track track) {
                Error((string)language["CMeET"]);
                return;
            }

            ReadableMetadata metadata = track.GetMetadata();
            if (metadata == null) {
                Error((string)language["CMeUT"]);
                return;
            }

            new CodecMetadata(metadata) {
                Title = (string)language["CMetT"]
            }.Show();
        }

        /// <summary>
        /// Show the post-render report in a popup.
        /// </summary>
        void ShowPostRenderReport(object _, RoutedEventArgs e) => MessageBox.Show(report, (string)language["PReRe"]);

        /// <summary>
        /// Shows a popup about what channel should be wired to which output.
        /// </summary>
        void DisplayWiring(object _, RoutedEventArgs e) {
            RenderTarget target = (RenderTarget)renderTarget.SelectedItem;
            ReferenceChannel[] channels = target.GetNameMappedChannels();
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            if (channels.Length > 8) {
                MessageBox.Show(string.Format((string)language["Over8"], string.Join(string.Empty, prototypes.Select(x => "\n- " + x.Name)),
                    (string)language["WrGui"]));
                return;
            }

            StringBuilder output = new StringBuilder();
            for (int i = 0; i < prototypes.Length; i++) {
                output.AppendLine(string.Format((string)language["ChCon"], prototypes[i].Name,
                    ChannelPrototype.Get(i, prototypes.Length).Name));
            }
            if (target is DownmixedRenderTarget downmix) {
                (ReferenceChannel source, ReferenceChannel posPhase, ReferenceChannel negPhase)[] matrixed = downmix.MatrixWirings;
                for (int i = 0; i < matrixed.Length; i++) {
                    output.AppendLine(string.Format((string)language["ChCMx"],
                        matrixed[i].source, matrixed[i].posPhase, matrixed[i].negPhase));
                }
            }
            MessageBox.Show(output.ToString(), (string)language["WrGui"]);
        }
    }
}