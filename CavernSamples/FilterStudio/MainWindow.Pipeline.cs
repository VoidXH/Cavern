using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Windows;

using VoidX.WPF;

using FilterStudio.Graphs;

namespace FilterStudio {
    // Handlers of the pipeline graph control
    partial class MainWindow {
        /// <summary>
        /// Clear the currently selected pipeline step (remove all its filters).
        /// </summary>
        void ClearStep(object sender, RoutedEventArgs e) {
            StyledNode node = pipeline.GetSelectedNode(sender);
            if (node == null) {
                Error((string)language["NPNod"]);
                return;
            }

            try {
                pipeline.Source.ClearSplitPoint(node.LabelText);
            } catch (ArgumentOutOfRangeException) {
                Error((string)language["NPiSi"]);
                return;
            }
            ReloadGraph(); // Force a reload of the filter graph
        }

        /// <summary>
        /// Delete the currently selected step from the pipeline.
        /// </summary>
        void DeleteStep(object sender, RoutedEventArgs e) {
            StyledNode node = pipeline.GetSelectedNode(sender);
            if (node == null) {
                Error((string)language["NPNod"]);
                return;
            }

            try {
                pipeline.Source.RemoveSplitPoint(node.LabelText);
            } catch (ArgumentOutOfRangeException) {
                Error((string)language["NPiSi"]);
                return;
            } catch (IndexOutOfRangeException) {
                Error((string)language["NLaSP"]);
                return;
            }

            pipeline.Source = pipeline.Source; // Force a reload of the pipeline graph
            ReloadGraph(); // Force a reload of the filter graph
        }

        /// <summary>
        /// Handle right-clicking on a pipeline <paramref name="element"/>.
        /// </summary>
        void PipelineRightClick(object element) {
            if (element is not Node && element is not Edge) {
                return;
            }

            List<(string, Action<object, RoutedEventArgs>)> menuItems = [
            ];
            if (element is Node) {
                menuItems.Add((null, null)); // Separator for deletion
                menuItems.Add(((string)language["CoCle"], (_, e) => ClearStep(element, e)));
                menuItems.Add(((string)language["CoDel"], (_, e) => DeleteStep(element, e)));
            }
            QuickContextMenu.Show(menuItems);
        }
    }
}