using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Windows;

using VoidX.WPF;

using FilterStudio.Graphs;

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
            ReloadGraph();
        }

        /// <summary>
        /// When the user lost the graph because it was moved outside the screen, this function redisplays it in the center of the frame.
        /// </summary>
        void Recenter(object _, RoutedEventArgs e) => ReloadGraph();

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
            properties.ItemsSource = new ObjectToDataGrid(node.Filter.Filter, FilterPropertyChanged, e => Error(e.Message));
        }

        /// <summary>
        /// Display the context menu when the graph is right clicked.
        /// </summary>
        void GraphRightClick(object element) {
            List<(string, Action<object, RoutedEventArgs>)> menuItems = [
                    ((string)language["FLabe"], (_, e) => AddLabel(element, e)),
                    ((string)language["FGain"], (_, e) => AddGain(element, e)),
                    ((string)language["FDela"], (_, e) => AddDelay(element, e)),
                    ((string)language["FBiqu"], (_, e) => AddBiquad(element, e)),
                ];
            if (element is Node) {
                menuItems.Add((null, null));
                menuItems.Add(((string)language["CoDel"], (_, e) => DeleteNode(element, e)));
            }
            QuickContextMenu.Show(menuItems);
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
    }
}