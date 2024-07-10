using Microsoft.Msagl.Drawing;
using System.Windows;
using System.Windows.Input;

using Cavern;
using Cavern.Filters;
using Cavern.QuickEQ.Equalization;
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
                BiquadEditor editor = new() {
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
        /// Add a new <see cref="GraphicEQ"/> to the graph.
        /// </summary>
        void AddGraphicEQ(object sender, RoutedEventArgs e) {
            string error = PreFilterAddingChecks(sender);
            if (error == null) {
                Equalizer eq = new();
                EQEditor editor = new(eq) {
                    Background = Background,
                    Resources = Resources
                };
                if (editor.ShowDialog().Value) {
                    FinalizeFilter(sender, new GraphicEQ(eq, Listener.DefaultSampleRate));
                }
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// Add a new <see cref="FastConvolver"/> to the graph.
        /// </summary>
        void AddConvolution(object sender, RoutedEventArgs e) {
            string error = PreFilterAddingChecks(sender);
            if (error == null) {
                ConvolutionEditor editor = new(null, Listener.DefaultSampleRate);
                if (editor.ShowDialog().Value && editor.Impulse != null) {
                    FinalizeFilter(sender, new FastConvolver(editor.Impulse));
                }
            } else {
                Error(error);
            }
        }

        /// <summary>
        /// Checks to perform before a filter can be added. If an error happens, returns its message, otherwise null.
        /// </summary>
        string PreFilterAddingChecks(object sender) {
            StyledNode node = graph.GetSelectedNode(sender);
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
            if (sender is StyledNode node) { // Context menu, node = parallel
                node.Filter.AddChild(filter);
            } else if (sender is Edge edge) { // Context menu, edge = inline
                ((StyledNode)graph.Graph.FindNode(edge.Source)).Filter.AddBeforeChildren(filter);
            } else if (filterShift.IsChecked || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                graph.SelectedNode.Filter.AddChild(filter);
            } else {
                graph.SelectedNode.Filter.AddBeforeChildren(filter);
            }
            ReloadGraph();
        }
    }
}