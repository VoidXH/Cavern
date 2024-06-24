using Microsoft.Msagl.Drawing;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Cavern;
using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.WPF;

using FilterStudio.Graphs;
using FilterStudio.Resources;

namespace FilterStudio {
    /// <summary>
    /// Main window of Cavern Filter Studio.
    /// </summary>
    public partial class MainWindow : Window {
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
            Settings.Default.SettingChanging += (_, e) => settingChanged |= !Settings.Default[e.SettingName].Equals(e.NewValue);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) => MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Save persistent settings on quit.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            Settings.Default.showInstructions = showInstructions.IsChecked;
            Settings.Default.graphDirection = (byte)graphDirection;
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
                    ConfigurationFile file = new EqualizerAPOConfigurationFile(dialog.FileName, Listener.DefaultSampleRate);

                    pipeline.Source = file;
                } catch {
                    Error((string)language["NLoad"]);
                }
            }
        }

        /// <summary>
        /// Export the configuration file to a new path.
        /// </summary>
        void ExportConfiguration(object _, RoutedEventArgs e) {
            if (pipeline.Source == null) {
                Error((string)language["NoCon"]);
                return;
            }

            // TODO: file picker
            ConfigurationFile export = new ConvolutionBoxFormat(pipeline.Source);
            export.Export(null);
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
        /// Update the name of a filter when any property of it was modified.
        /// </summary>
        void FilterPropertyChanged() {
            StyledNode node = graph.SelectedNode;
            Filter modified = node?.Filter?.Filter;
            if (modified != null) {
                string newDisplayName = modified.ToString();
                if (node.LabelText != newDisplayName) {
                    node.LabelText = newDisplayName;
                    selectedNode.Text = node.LabelText;
                    ReloadGraph();
                    graph.SelectNode(node.Id);
                }
            }
        }

        /// <summary>
        /// Get the channels that are used in the current configuration or will be used when a new configuration will be created.
        /// </summary>
        ReferenceChannel[] GetChannels() => rootNodes == null ? channels : rootNodes.Select(x => ((InputChannel)x.Filter).Channel).ToArray();
    }
}