using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;
using VoidX.WPF;

using FilterStudio.Graphs;
using FilterStudio.Windows;

namespace FilterStudio {
    // Handlers of the filter graph control
    partial class MainWindow {
        /// <summary>
        /// The direction where the graph tree is layed out.
        /// </summary>
        LayerDirection graphDirection;

        /// <summary>
        /// Change the direction where the graph tree is layed out to top to bottom.
        /// </summary>
        void SetDirectionTB(object _, RoutedEventArgs e) => SetDirection(LayerDirection.TB);

        /// <summary>
        /// Change the direction where the graph tree is layed out to left to right.
        /// </summary>
        void SetDirectionLR(object _, RoutedEventArgs e) => SetDirection(LayerDirection.LR);

        /// <summary>
        /// Change the direction where the graph tree is layed out to bottom to top.
        /// </summary>
        void SetDirectionBT(object _, RoutedEventArgs e) => SetDirection(LayerDirection.BT);

        /// <summary>
        /// Change the direction where the graph tree is layed out to right to left.
        /// </summary>
        void SetDirectionRL(object _, RoutedEventArgs e) => SetDirection(LayerDirection.RL);

        /// <summary>
        /// Change the direction where the graph tree is layed out.
        /// </summary>
        void SetDirection(LayerDirection direction) {
            graphDirection = direction;
            dirTB.IsChecked = direction == LayerDirection.TB;
            dirLR.IsChecked = direction == LayerDirection.LR;
            dirBT.IsChecked = direction == LayerDirection.BT;
            dirRL.IsChecked = direction == LayerDirection.RL;
            ReloadGraph();
        }

        /// <summary>
        /// When the user lost the graph because it was moved outside the screen, this function redisplays it in the center of the frame.
        /// </summary>
        void Recenter(object _, RoutedEventArgs e) => pipeline.Source = pipeline.Source;

        /// <summary>
        /// Converts all filters to convolutions and merges them downwards if they only have a single child.
        /// </summary>
        void ConvertToConvolution(object _, RoutedEventArgs e) {
            if (pipeline.Source == null) {
                Error((string)language["NoCon"]);
                return;
            }

            ConvolutionLengthDialog length = new();
            if (length.ShowDialog().Value) {
                pipeline.Source.SplitPoints[0].roots.ConvertToConvolution(length.Size);
                ReloadGraph();
            }
        }

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
        /// Delete the selected edge.
        /// </summary>
        void DeleteEdge(Edge edge) {
            FilterGraphNode parent = ((StyledNode)edge.SourceNode).Filter,
                child = ((StyledNode)edge.TargetNode).Filter;
            if (parent.Children.Count == 1 && child.Parents.Count == 1 && parent.Filter is InputChannel && child.Filter is OutputChannel) {
                Error((string)language["NLaEd"]);
                return;
            }
            parent.DetachChild(child, false);

            if (child.Parents.Count == 0) {
                int lastNode = rootNodes.Length;
                Array.Resize(ref rootNodes, lastNode + 1);
                rootNodes[lastNode] = child;
            }
            ReloadGraph();
        }

        /// <summary>
        /// When selecting a node, open it for modification.
        /// </summary>
        void GraphLeftClick(object _) {
            StyledNode node = graph.SelectedNode;
            if (node == null || node.Filter == null) {
                selectedNode.Text = (string)language["NNode"];
                properties.ItemsSource = Array.Empty<object>();
                return;
            }

            selectedNode.Text = node.LabelText;
            if (properties.ItemsSource is ObjectToDataGrid old) {
                properties.BeginningEdit -= old.BeginningEdit;
            }
            ObjectToDataGrid propertySource = new ObjectToDataGrid(node.Filter.Filter, FilterPropertyChanged, e => Error(e.Message),
                (typeof(Equalizer), EditEqualizer));
            properties.ItemsSource = propertySource;
            properties.BeginningEdit += propertySource.BeginningEdit;
        }

        /// <summary>
        /// Display the context menu when the graph is right clicked.
        /// </summary>
        void GraphRightClick(object element) {
            if (element is not Node && element is not Edge) {
                return;
            }

            List<(string, Action<object, RoutedEventArgs>)> menuItems = [
                    ((string)language["FLabe"], (_, e) => AddLabel(element, e)),
                    ((string)language["FGain"], (_, e) => AddGain(element, e)),
                    ((string)language["FDela"], (_, e) => AddDelay(element, e)),
                    ((string)language["FBiqu"], (_, e) => AddBiquad(element, e)),
                    (null, null) // Separator for deletion
                ];
            if (element is Node) {
                menuItems.Add(((string)language["CoDel"], (_, e) => DeleteNode(element, e)));
            } else {
                menuItems.Add(((string)language["CoDel"], (_, __) => DeleteEdge((Edge)element)));
            }
            QuickContextMenu.Show(menuItems);
        }

        /// <summary>
        /// Handle creating a user-selected connection.
        /// </summary>
        void GraphConnect(StyledNode parent, StyledNode child) {
            if (child.Filter.Filter is InputChannel) {
                Error((string)language["NCInp"]);
                return;
            }
            if (parent.Filter.Filter is OutputChannel) {
                Error((string)language["NCOut"]);
                return;
            }
            if (parent.Filter.Children.Contains(child.Filter)) {
                return; // Would be a duplicate edge
            }

            if (child.Filter.Parents.Count == 0) {
                ArrayExtensions.Remove(ref rootNodes, child.Filter);
            }

            parent.Filter.AddChild(child.Filter);
            if (rootNodes.HasCycles()) {
                Error((string)language["NLoop"]);
                parent.Filter.DetachChild(child.Filter, false);
            } else {
                ReloadGraph();
            }
        }

        /// <summary>
        /// Updates the graph based on the <see cref="rootNodes"/>.
        /// </summary>
        void ReloadGraph() {
            if (rootNodes != null) {
                Graph newGraph = Parsing.ParseConfigurationFile(rootNodes);
                newGraph.Attr.BackgroundColor = pipeline.background;
                newGraph.Attr.LayerDirection = graphDirection;
                graph.Graph = newGraph;
            }
        }

        /// <summary>
        /// Open the editor for an existing <see cref="Equalizer"/>.
        /// </summary>
        void EditEqualizer(object filter) {
            Equalizer equalizer = (Equalizer)filter;
            // TODO: editor
        }
    }
}