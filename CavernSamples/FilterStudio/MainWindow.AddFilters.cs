using Microsoft.Msagl.Drawing;
using System.Windows;
using System.Windows.Input;

using Cavern;
using Cavern.Filters;
using Cavern.WPF;

using FilterStudio.Graphs;

namespace FilterStudio {
    // Click handlers of each item under "Add filter"
    partial class MainWindow {
        /// <summary>
        /// Add a new <see cref="BypassFilter"/> to the graph to be used as a label.
        /// </summary>
        void AddLabel(object sender, RoutedEventArgs e) => AddFilter(sender, new BypassFilter((string)language["NLabe"]));

        /// <summary>
        /// Add a new <see cref="Gain"/> filter to the graph.
        /// </summary>
        void AddGain(object sender, RoutedEventArgs e) => AddFilter(sender, new Gain(0));

        /// <summary>
        /// Add a new <see cref="Delay"/> filter to the graph.
        /// </summary>
        void AddDelay(object sender, RoutedEventArgs e) => AddFilter(sender, new Delay(0, Listener.DefaultSampleRate));

        /// <summary>
        /// Add a new <see cref="BiquadFilter"/> to the graph.
        /// </summary>
        void AddBiquad(object sender, RoutedEventArgs e) {
            string error = PreFilterAddingChecks(sender);
            if (error == null) {
                BiquadEditor editor = new BiquadEditor {
                    Background = Background,
                    Resources = Resources
                };
                if (editor.ShowDialog().Value) {
                    FinalizeFilter(sender, editor.Filter);
                }
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// Context menu options pass the selected node in their <paramref name="sender"/> parameter. Use this function to get the actually
        /// selected node, not the one that was last left-clicked.
        /// </summary>
        StyledNode GetSelectedNode(object sender) {
            if (sender is IViewerNode hoverNode) { // Context menu, node = parallel
                return (StyledNode)hoverNode.Node;
            } else if (sender is IViewerEdge edge) { // Context menu, edge = inline
                return (StyledNode)viewer.Graph.FindNode(edge.Edge.Source);
            } else { // Window
                return SelectedFilter;
            }
        }

        /// <summary>
        /// Checks to perform before a filter can be added. If an error happens, returns its message, otherwise null.
        /// </summary>
        string PreFilterAddingChecks(object sender) {
            StyledNode node = GetSelectedNode(sender);
            if (node == null) {
                return (string)language["NNode"];
            } else if (node.Filter.Filter is OutputChannel) {
                return (string)language["NFLas"];
            }
            return null;
        }

        /// <summary>
        /// Add a basic filter after the currently selected graph node.
        /// </summary>
        void AddFilter(object sender, Filter filter) {
            string error = PreFilterAddingChecks(sender);
            if (error == null) {
                FinalizeFilter(sender, filter);
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// When the <see cref="PreFilterAddingChecks"/> have passed, and the filter has been determined, add it to the graph.
        /// </summary>
        void FinalizeFilter(object sender, Filter filter) {
            if (sender is IViewerNode node) { // Context menu, node = parallel
                ((StyledNode)node.Node).Filter.AddChild(filter);
            } else if (sender is IViewerEdge edge) { // Context menu, edge = inline
                ((StyledNode)viewer.Graph.FindNode(edge.Edge.Source)).Filter.AddBeforeChildren(filter);
            } else if (filterShift.IsChecked || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                SelectedFilter.Filter.AddChild(filter);
            } else {
                SelectedFilter.Filter.AddBeforeChildren(filter);
            }
            ReloadGraph();
        }
    }
}