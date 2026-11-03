using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.Utilities;
using Cavern.Filters.Utilities;
using Cavern.WPF;

using FilterStudio.Resources;
using FilterStudio.Windows;

namespace FilterStudio {
    // Click handlers of each item under "File"
    partial class MainWindow {
        /// <summary>
        /// Create a new empty configuration.
        /// </summary>
        void NewConfiguration(object _, RoutedEventArgs e) {
            pipeline.Source = new CavernFilterStudioConfigurationFile((string)language["NSNew"], channels);
            ReloadGraph();
        }

        /// <summary>
        /// Open a configuration file of known formats.
        /// </summary>
        void LoadConfiguration(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = (string)language["OpFil"]
            };
            if (dialog.ShowDialog().Value) {
                OpenConfigurationFile(dialog.FileName);
            }
        }

        /// <summary>
        /// Open a configuration file of known formats from <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path of the configuration file to open</param>
        void OpenConfigurationFile(string path) {
            try {
                ConfigurationFileType type = ConfigurationFileType.EqualizerAPO;
                using (FileStream file = File.OpenRead(path)) {
                    int magicNumber = file.ReadInt32();
                    if (magicNumber == 0x3CBFBBEF) {
                        type = ConfigurationFileType.CavernFilterStudio;
                    } else if (magicNumber == ConvolutionBoxFormatConfigurationFile.syncWord) {
                        type = ConfigurationFileType.ConvolutionBoxFormat;
                    }
                }

                ConfigurationFile import = type switch {
                    ConfigurationFileType.CavernFilterStudio => new CavernFilterStudioConfigurationFile(path),
                    ConfigurationFileType.ConvolutionBoxFormat => new ConvolutionBoxFormatConfigurationFile(path),
                    _ => new EqualizerAPOConfigurationFile(path, SampleRate)
                };

                // Format-specific error checks
                if (import is ConvolutionBoxFormatConfigurationFile cbf) {
                    sampleRate.SelectedItem = cbf.SampleRate;
                    if ((int)sampleRate.SelectedItem != cbf.SampleRate) {
                        Error((string)language["NUnSR"]);
                    }
                }

                pipeline.Source = import;
            } catch (Exception ex) {
                Error(string.Format((string)language["NLoad"], ex.Message));
            }
        }

        /// <summary>
        /// Export the configuration file to a new path in <see cref="CavernFilterStudioConfigurationFile"/>.
        /// </summary>
        void ExportCavernFilterStudio(object source, RoutedEventArgs e) => ExportConfiguration(source, () => new CavernFilterStudioConfigurationFile(pipeline.Source));

        /// <summary>
        /// Export the configuration file to a new path in <see cref="ConvolutionBoxFormatConfigurationFile"/>.
        /// </summary>
        void ExportConvolutionBoxFormat(object source, RoutedEventArgs e) =>
            ExportConfiguration(source, () => new ConvolutionBoxFormatConfigurationFile(pipeline.Source, SampleRate));

        /// <summary>
        /// Export the configuration file to a new path in <see cref="EqualizerAPOConfigurationFile"/>.
        /// </summary>
        void ExportEqualizerAPO(object source, RoutedEventArgs e) => ExportConfiguration(source, () => new EqualizerAPOConfigurationFile(pipeline.Source));

        /// <summary>
        /// Makes sure the configuration file is ready for export, returns true if it is.
        /// </summary>
        bool ExportChecks() {
            int systemSampleRate = SampleRate;
            HashSet<IFilterGraphNode> map = pipeline.Source.SplitPoints[0].Roots.MapGraph();
            Filter mismatch = map.FirstOrDefault(x => x.Filter != null &&
                x.Filter is ISampleRateDependentFilter sr && sr.SampleRate != systemSampleRate)?.Filter;
            if (mismatch != null && !WarningYesNo(string.Format((string)language["WSaRe"], FilterToString(mismatch), systemSampleRate,
                ((ISampleRateDependentFilter)mismatch).SampleRate))) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Export the configuration file to a new path.
        /// </summary>
        /// <param name="generator">Creates the <see cref="ConfigurationFile"/> for export in the target format</param>
        void ExportConfiguration(object source, Func<ConfigurationFile> generator) {
            if (pipeline.Source == null) {
                Error((string)language["NoCon"]);
                return;
            } else if (!ExportChecks()) {
                return;
            }

            ConfigurationFile file;
            try {
                file = generator();
            } catch (Exception e) {
                Error(e.Message);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog() {
                FileName = $"{file.SplitPoints[0].Name}.{file.FileExtension}",
                Filter = $"{((MenuItem)source).Header}|*.{file.FileExtension}"
            };
            if (dialog.ShowDialog().Value) {
                try {
                    file.Export(dialog.FileName);
                    Success((string)language["ExSuc"]);
                } catch (UnsupportedFilterForExportException e) {
                    Error(string.Format((string)language["NUnFi"], FilterToString(e.Filter)));
                } catch (Exception e) {
                    Error(e.Message);
                }
            }
        }

        /// <summary>
        /// Select the channels that are available in the system.
        /// </summary>
        void SelectChannels(object _, RoutedEventArgs e) {
            if (pipeline.Source != null) {
                Error((string)language["NOpen"]);
            }

            ChannelSelector dialog = new(this) {
                SelectedChannels = channels
            };
            if (dialog.ShowDialog().Value) {
                ReferenceChannel[] newChannels = dialog.SelectedChannels;
                if (newChannels.Length != 0) {
                    channels = newChannels;
                    Settings.Default.channels = channels.GetMask();
                } else {
                    Error((string)language["NChan"]);
                }
            }
        }

        /// <summary>
        /// Open the settings window.
        /// </summary>
        void OpenSettings(object _, RoutedEventArgs e) {
            SettingsDialog dialog = new();
            if (dialog.ShowDialog().Value) {
                settingChanged = true;
            }
        }
    }
}
