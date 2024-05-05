using Microsoft.Win32;
using System;
using System.Windows;

using Cavern;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

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
        /// Each channel's full filter graph.
        /// </summary>
        (string name, FilterGraphNode root)[] rootNodes;

        /// <summary>
        /// Any setting has changed in the application and it should be saved.
        /// </summary>
        bool settingChanged;

        /// <summary>
        /// Main window of Cavern Filter Studio.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            graph.OnLeftClick += GraphLeftClick;
            graph.OnRightClick += GraphRightClick;

            showInstructions.IsChecked = Settings.Default.showInstructions;
            SetInstructions(null, null);
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
            if (settingChanged) {
                Settings.Default.Save();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// Create a new empty configuration.
        /// </summary>
        void NewConfiguration(object _, RoutedEventArgs e) {
            rootNodes = new CavernFilterStudioConfigurationFile(8).InputChannels;
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
                ConfigurationFile file = new EqualizerAPOConfigurationFile(dialog.FileName, Listener.DefaultSampleRate);

                rootNodes = file.InputChannels;
                ReloadGraph();
            }
        }

        /// <summary>
        /// When the user lost the graph because it was moved outside the screen, this function redisplays it in the center of the frame.
        /// </summary>
        void Recenter(object _, RoutedEventArgs e) => ReloadGraph();

        /// <summary>
        /// Delete the currently selected node.
        /// </summary>
        void DeleteNode(object sender, RoutedEventArgs e) {
            StyledNode node = graph.GetSelectedNode(sender);
            if (node == null || node.Filter == null) {
                Error((string)language["NFNod"]);
            } else if (node.Filter.Filter is InputChannel) {
                Error((string)language["NFInp"]);
            } else if (node.Filter.Filter is OutputChannel) {
                Error((string)language["NFOut"]);
            } else {
                node.Filter.DetachFromGraph();
                ReloadGraph();
            }
        }

        /// <summary>
        /// Handle when the instructions are enabled or disabled.
        /// </summary>
        void SetInstructions(object _, RoutedEventArgs e) {
            Visibility instructions = showInstructions.IsChecked ? Visibility.Visible : Visibility.Hidden;
            help1.Visibility = instructions;
            help2.Visibility = instructions;
        }

        /// <summary>
        /// Shows information about the used Cavern library and its version.
        /// </summary>
        void About(object _, RoutedEventArgs e) => MessageBox.Show(Listener.Info, (string)language["HAbou"]);

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
    }
}