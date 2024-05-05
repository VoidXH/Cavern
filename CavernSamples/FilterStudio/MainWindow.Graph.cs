using Microsoft.Msagl.Drawing;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System;

using VoidX.WPF;

using FilterStudio.Graphs;

namespace FilterStudio {
    // Handlers of the filter graph control
    partial class MainWindow {
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
                graph.Graph = Parsing.ParseConfigurationFile(rootNodes, Parsing.ParseBackground((SolidColorBrush)Background));
            }
        }
    }
}