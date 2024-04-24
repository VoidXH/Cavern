using Microsoft.Win32;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Cavern;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;

using FilterStudio.Graphs;

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
        /// The currently selected filter's node.
        /// </summary>
        StyledNode SelectedFilter => (StyledNode)graph.Graph?.Nodes.FirstOrDefault(x => x.Attr.LineWidth > 1);

        /// <summary>
        /// Each channel's full filter graph.
        /// </summary>
        (string name, FilterGraphNode root)[] rootNodes;

        /// <summary>
        /// Main window of Cavern Filter Studio.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        static void Error(string message) => MessageBox.Show(message, (string)language["Error"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// When selecting a <paramref name="node"/>, open it for modification.
        /// </summary>
        void OnNodeSelected() {
            StyledNode node = SelectedFilter;
            if (node == null) {
                selectedNode.Text = (string)language["NNode"];
                return;
            }
            selectedNode.Text = node.LabelText;
        }

        /// <summary>
        /// Updates the graph based on the <see cref="rootNodes"/>.
        /// </summary>
        void ReloadGraph() {
            graph.Graph = Parsing.ParseConfigurationFile(rootNodes, Parsing.ParseBackground((SolidColorBrush)Background));
            OnNodeSelected();
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
        /// Delete the currently selected node.
        /// </summary>
        void DeleteNode(object _, RoutedEventArgs e) {
            StyledNode node = SelectedFilter;
            if (node == null || node.Filter == null) {
                Error((string)language["NFNod"]);
                return;
            }

            node.Filter.DetachFromGraph();
            ReloadGraph();
        }

        /// <summary>
        /// Hack to provide a Click event for MSAGL's WPF library.
        /// </summary>
        void GraphClick(object _, MouseButtonEventArgs e) {
            Dispatcher.BeginInvoke(() => { // Call after the graph has handled it
                OnNodeSelected();
            });
        }
    }
}