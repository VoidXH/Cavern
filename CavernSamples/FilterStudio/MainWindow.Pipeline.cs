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
        /// Add a new empty pipeline step after the selected node.
        /// </summary>
        void AddStep(object sender, RoutedEventArgs e) {
            StyledNode node = pipeline.GetSelectedNode(sender);
            if (node == null) {
                Error((string)language["NPNod"]);
                return;
            }

            if (!int.TryParse(node.Id, out int uid)) {
                if (node.Id == PipelineEditor.inNodeUid) {
                    Error((string)language["NPNod"]);
                    return;
                } else {
                    uid = pipeline.Source.SplitPoints.Count;
                }
            }

            pipeline.Source.AddSplitPoint(uid, (string)language["NSNew"]);
            pipeline.Source = pipeline.Source; // Force a reload of the pipeline graph
            pipeline.SelectNode(uid.ToString()); // Force a reload of the filter graph on the new step
        }

        /// <summary>
        /// Clear the currently selected pipeline step (remove all its filters).
        /// </summary>
        void ClearStep(object sender, RoutedEventArgs e) {
            StyledNode node = pipeline.GetSelectedNode(sender);
            if (node == null) {
                Error((string)language["NPNod"]);
                return;
            }

            if (int.TryParse(node.Id, out int uid)) {
                pipeline.Source.ClearSplitPoint(uid);
            } else {
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
            if (element is not Node) {
                return;
            }

            List<(string, Action<object, RoutedEventArgs>)> menuItems = [
                ((string)language["OpAdP"], (_, e) => AddStep(element, e)),
                (null, null), // Separator for deletion
                ((string)language["CoCle"], (_, e) => ClearStep(element, e)),
                ((string)language["CoDel"], (_, e) => DeleteStep(element, e))
            ];
            QuickContextMenu.Show(menuItems);
        }
    }
}