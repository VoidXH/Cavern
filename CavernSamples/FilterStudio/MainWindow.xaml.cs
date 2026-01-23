using Microsoft.Msagl.Drawing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cavern;
using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.Utilities;
using Cavern.Utilities;
using Cavern.WPF;

using FilterStudio.Graphs;
using FilterStudio.Resources;

namespace FilterStudio {
    /// <summary>
    /// Main window of Cavern Filter Studio.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// The currently selected project sample rate.
        /// </summary>
        int SampleRate => (int)sampleRate.SelectedItem;

        /// <summary>
        /// Source of language strings.
        /// </summary>
        static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        /// <summary>
        /// The user-selected channels used when new configurations are created.
        /// </summary>
        ReferenceChannel[] channels;

        /// <summary>
        /// Each channel's full filter graph.
        /// </summary>
        FilterGraphNode[] rootNodes;

        /// <summary>
        /// Any setting has changed in the application and it should be saved.
        /// </summary>
        bool settingChanged;

        /// <summary>
        /// Main window of Cavern Filter Studio.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            pipeline.OnRightClick += PipelineRightClick;
            pipeline.OnSplitChanged += SplitChanged;
            pipeline.background = Parsing.ParseBackground((SolidColorBrush)Background);
            pipeline.language = language;
            graph.OnLeftClick += GraphLeftClick;
            graph.OnRightClick += GraphRightClick;
            graph.OnConnect += GraphConnect;

            showInstructions.IsChecked = Settings.Default.showInstructions;
            SetInstructions(null, null);
            SetDirection((LayerDirection)Settings.Default.graphDirection);
            channels = ReferenceChannelExtensions.FromMask(Settings.Default.channels);
            sampleRate.ItemsSource = sampleRates;
            sampleRate.SelectedItem = Settings.Default.sampleRate;
            Settings.Default.SettingChanging += (_, e) => settingChanged |= !Settings.Default[e.SettingName].Equals(e.NewValue);
        }

        /// <summary>
        /// If a filter can be translated to the user's language, return that instead of a regular ToString.
        /// </summary>
        static string FilterToString(Filter filter) =>
            filter is ILocalizableToString loc ? loc.ToString(CultureInfo.CurrentCulture) : filter.ToString();

        /// <summary>
        /// Displays a message for a successful operation.
        /// </summary>
        static void Success(string message) =>
            MessageBox.Show(message, (string)language["Succe"], MessageBoxButton.OK, MessageBoxImage.Information);

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) => MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Warns the user about something potentially destructive happening, and asks them if they want to continue.
        /// </summary>
        static bool Warning(string question) =>
            MessageBox.Show(question, (string)language["Warni"], MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.showInstructions = showInstructions.IsChecked;
            Settings.Default.graphDirection = (byte)graphDirection;
            Settings.Default.sampleRate = (int)sampleRate.SelectedItem;
            if (settingChanged) {
                Settings.Default.Save();
            }
            base.OnClosed(e);
        }

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
                try {
                    ConfigurationFileType type = ConfigurationFileType.EqualizerAPO;
                    using (FileStream file = File.OpenRead(dialog.FileName)) {
                        int magicNumber = file.ReadInt32();
                        if (magicNumber == 0x3CBFBBEF) {
                            type = ConfigurationFileType.CavernFilterStudio;
                        } else if (magicNumber == ConvolutionBoxFormatConfigurationFile.syncWord) {
                            type = ConfigurationFileType.ConvolutionBoxFormat;
                        }
                    }

                    ConfigurationFile import = type switch {
                        ConfigurationFileType.CavernFilterStudio => new CavernFilterStudioConfigurationFile(dialog.FileName),
                        ConfigurationFileType.ConvolutionBoxFormat => new ConvolutionBoxFormatConfigurationFile(dialog.FileName),
                        _ => new EqualizerAPOConfigurationFile(dialog.FileName, SampleRate)
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
        }

        /// <summary>
        /// Export the configuration file to a new path in <see cref="CavernFilterStudioConfigurationFile"/>.
        /// </summary>
        void ExportCavernFilterStudio(object source, RoutedEventArgs e) =>
            ExportConfiguration(source, () => new CavernFilterStudioConfigurationFile(pipeline.Source));

        /// <summary>
        /// Export the configuration file to a new path in <see cref="ConvolutionBoxFormatConfigurationFile"/>.
        /// </summary>
        void ExportConvolutionBoxFormat(object source, RoutedEventArgs e) =>
            ExportConfiguration(source, () => new ConvolutionBoxFormatConfigurationFile(pipeline.Source, SampleRate));

        /// <summary>
        /// Export the configuration file to a new path in <see cref="EqualizerAPOConfigurationFile"/>.
        /// </summary>
        void ExportEqualizerAPO(object source, RoutedEventArgs e) =>
            ExportConfiguration(source, () => new EqualizerAPOConfigurationFile(pipeline.Source));

        /// <summary>
        /// Makes sure the configuration file is ready for export, returns true if it is.
        /// </summary>
        bool ExportChecks() {
            int systemSampleRate = SampleRate;
            HashSet<FilterGraphNode> map = pipeline.Source.SplitPoints[0].Roots.MapGraph();
            Filter mismatch = map.FirstOrDefault(x => x.Filter != null &&
                x.Filter is ISampleRateDependentFilter sr && sr.SampleRate != systemSampleRate)?.Filter;
            if (mismatch != null && !Warning(string.Format((string)language["WSaRe"], FilterToString(mismatch), systemSampleRate,
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

            ChannelSelector dialog = new() {
                Background = Background,
                Resources = Resources,
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
        /// Handle when the instructions are enabled or disabled.
        /// </summary>
        void SetInstructions(object _, RoutedEventArgs e) {
            Visibility instructions = showInstructions.IsChecked ? Visibility.Visible : Visibility.Hidden;
            help1.Visibility = instructions;
            help2.Visibility = instructions;
            help3.Visibility = instructions;
        }

        /// <summary>
        /// Shows information about the used Cavern library and its version.
        /// </summary>
        void About(object _, RoutedEventArgs e) => MessageBox.Show(Listener.Info, (string)language["HAbou"]);

        /// <summary>
        /// Open Cavern's website.
        /// </summary>
        void Ad(object _, RoutedEventArgs e) => Process.Start(new ProcessStartInfo {
            FileName = "https://cavern.sbence.hu",
            UseShellExecute = true
        });

        /// <summary>
        /// A different split of the edited file is selected.
        /// </summary>
        void SplitChanged(FilterGraphNode[] splitRoots) {
            rootNodes = splitRoots;
            ReloadGraph();
        }

        /// <summary>
        /// Update the UI to the changes that happened in the filter.
        /// </summary>
        void FilterPropertyChanged() {
            StyledNode node = graph.SelectedNode;
            Filter modified = node?.Filter?.Filter;
            if (modified != null) {
                string newDisplayName = modified.ToString();
                if (node.LabelText != newDisplayName) { // If the displayed name changed, reload the graph
                    node.LabelText = newDisplayName;
                    selectedNode.Text = node.LabelText;
                    ReloadGraph();
                }
                // Otherwise only the property panel
                graph.SelectNode(node.Id);
            }
        }

        /// <summary>
        /// Get the channels that are used in the current configuration or will be used when a new configuration will be created.
        /// </summary>
        ReferenceChannel[] GetChannels() => rootNodes == null ? channels : [.. rootNodes.Select(x => ((InputChannel)x.Filter).Channel)];

        /// <summary>
        /// All possible project sample rates.
        /// </summary>
        static readonly int[] sampleRates = [8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 176400, 192000, 384000];
    }
}
