using EQAPOtoFIR.Dialogs;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Cavern.Format;

namespace EQAPOtoFIR {
    /// <summary>
    /// Interaction logic for the application window.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Last parsed Equalizer APO config file.
        /// </summary>
        ConfigParser parser;

        /// <summary>
        /// Initialize the application window.
        /// </summary>
        public MainWindow() => InitializeComponent();

        /// <summary>
        /// Ask the user for a configuration file.
        /// </summary>
        void Open(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                InitialDirectory = "C:\\Program Files\\EqualizerAPO\\config",
                Filter = "Equalizer APO configuration files (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true) {
                configFile.Content = Path.GetFileName(dialog.FileName);
                parser = new ConfigParser(dialog.FileName);
                export.IsEnabled = true;
            }
        }

        /// <summary>
        /// Perform the export process when WAVE is selected.
        /// </summary>
        void ExportWAV(BitDepth bits, ExportFormat format) {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "Channel_ALL.wav",
                Filter = "RIFF WAVE files (*.wav)|*.wav"
            };
            if (dialog.ShowDialog() == true) {
                parser.ExportWAV(Path.GetDirectoryName(dialog.FileName), format, bits, sampleRate.Value, fftSize.Value,
                    minimum.IsChecked.Value);
            }
        }

        /// <summary>
        /// Perform the export process when C array is selected.
        /// </summary>
        void ExportC(BitDepth bits, ExportFormat format) {
            int segments = 1;
            if (c.IsChecked.Value) {
                SegmentsDialog segmentDialog = new SegmentsDialog();
                if (segmentDialog.ShowDialog() == true) {
                    segments = segmentDialog.Segments;
                } else {
                    return;
                }
            }

            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "Channel_ALL.c",
                Filter = "C source files (*.c)|*.c"
            };
            if (dialog.ShowDialog() == true) {
                parser.ExportC(Path.GetDirectoryName(dialog.FileName), format, bits, sampleRate.Value, fftSize.Value,
                    minimum.IsChecked.Value, segments);
            }
        }

        /// <summary>
        /// Perform the export process when HLS optimization is selected.
        /// </summary>
        void ExportHLS(BitDepth bits, ExportFormat format) {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "Channel_ALL.txt",
                Filter = "Text files (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true) {
                parser.ExportHLS(Path.GetDirectoryName(dialog.FileName), format, bits, sampleRate.Value, fftSize.Value,
                    minimum.IsChecked.Value);
            }
        }

        /// <summary>
        /// Perform the export process when MultEQ-X (PEQ only) is selected.
        /// </summary>
        void ExportMQX() {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "MultEQ Configuration.mqx",
                Filter = "MultEQ-X files (*.mqx)|*.mqx"
            };
            parser.ExportMQX(dialog, sampleRate.Value);
        }

        /// <summary>
        /// Perform the export process when MultEQ-X (full approximation) is selected.
        /// </summary>
        void ExportMQXSim() {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "MultEQ Configuration.mqx",
                Filter = "MultEQ-X files (*.mqx)|*.mqx"
            };
            parser.ExportMQXSim(dialog, sampleRate.Value);
        }

        /// <summary>
        /// Select a location and export the results.
        /// </summary>
        void Export(object sender, RoutedEventArgs e) {
            BitDepth bits = BitDepth.Float32;
            if (int8.IsChecked.Value) {
                bits = BitDepth.Int8;
            } else if (int16.IsChecked.Value) {
                bits = BitDepth.Int16;
            } else if (int24.IsChecked.Value) {
                bits = BitDepth.Int24;
            }
            ExportFormat format = impulse.IsChecked.Value ? ExportFormat.Impulse : ExportFormat.FIR;

            if (wav.IsChecked.Value) {
                ExportWAV(bits, format);
            } else if (c.IsChecked.Value) {
                ExportC(bits, format);
            } else if (hls.IsChecked.Value) {
                ExportHLS(bits, format);
            } else if (multEQX_PEQ.IsChecked.Value) {
                ExportMQX();
            } else {
                ExportMQXSim();
            }
        }

        private void Ad_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "https://en.sbence.hu/",
            UseShellExecute = true
        });
    }
}