using Microsoft.Msagl.Drawing;
using Microsoft.Win32;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Cavern;
using Cavern.Format.ConfigurationFile;

using FilterStudio.Graphs;

namespace FilterStudio {
    public partial class MainWindow : Window {
        /// <summary>
        /// Source of language strings.
        /// </summary>
        static readonly ResourceDictionary language = Consts.Language.GetMainWindowStrings();

        public MainWindow() {
            InitializeComponent();
            Graph show = new();
            graph.Graph = show;
            System.Windows.Media.Color sourceColor = ((SolidColorBrush)Background).Color;
            show.Attr.BackgroundColor = new Microsoft.Msagl.Drawing.Color(sourceColor.R, sourceColor.G, sourceColor.B);
            show.AddNode(new StyledNode("A", "Root"));
            show.AddNode(new StyledNode("B", "Child"));
            new StyledEdge(show, "A", "B");
        }

        void OnNodeSelected(Node node) {
            if (node == null) {
                selectedNode.Text = (string)language["NNode"];
                return;
            }
            selectedNode.Text = node.LabelText;
        }

        void LoadConfiguration(object _, RoutedEventArgs e) {
            OpenFileDialog dialog = new() {
                Filter = (string)language["OpFil"]
            };
            if (dialog.ShowDialog().Value) {
                ConfigurationFile file = new EqualizerAPOConfigurationFile(dialog.FileName, Listener.DefaultSampleRate);
            }
        }

        void GraphClick(object _, MouseButtonEventArgs e) {
            Dispatcher.BeginInvoke(() => { // Call after the graph has handled it
                OnNodeSelected(graph.Graph.Nodes.FirstOrDefault(x => x.Attr.LineWidth > 1));
            });
        }
    }
}