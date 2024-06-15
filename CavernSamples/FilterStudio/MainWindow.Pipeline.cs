using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Cavern;
using Cavern.Format.ConfigurationFile.Presets;
using Cavern.QuickEQ.Crossover;
using VoidX.WPF;

using FilterStudio.Graphs;
using FilterStudio.Windows.PipelineSteps;

namespace FilterStudio {
    // Handlers of the pipeline graph control
    partial class MainWindow {
        /// <summary>
        /// Add a new empty pipeline step after the selected node.
        /// </summary>
        void AddStep(object sender, RoutedEventArgs e) => PipelineAddition(sender, (uid) => {
            pipeline.Source.AddSplitPoint(uid, (string)language["NSNew"]);
            return true;
        });

        /// <summary>
        /// Add a new crossover step after the selected node.
        /// </summary>
        void AddCrossover(object sender, RoutedEventArgs e) => PipelineAddition(sender, (uid) => {
            CrossoverDialog dialog = new(GetChannels());
            if (!dialog.ShowDialog().Value) {
                return false;
            }

            CrossoverFilterSet crossover = new((string)language["LabXO"], dialog.Type, Listener.DefaultSampleRate, 65536, dialog.Target,
                dialog.Crossovers.Where(x => x.frequency != null).Select(x => (x.channel, (float)x.frequency)).ToArray());
            crossover.Add(pipeline.Source, uid);
            return true;
        });

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
        /// Add a new step in the pipeline before the currently selected node.
        /// </summary>
        /// <param name="sender">Either the selected node or a control if not called from a right-click menu</param>
        /// <param name="addBeforeUid">Tries to add the step to the pipeline and returns if the addition was successful</param>
        void PipelineAddition(object sender, Func<int, bool> addBeforeUid) {
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

            if (addBeforeUid(uid)) {
                pipeline.Source = pipeline.Source; // Force a reload of the pipeline graph
                pipeline.SelectNode(uid.ToString()); // Force a reload of the filter graph on the new step
            }
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
                ((string)language["OpAdC"], (_, e) => AddCrossover(element, e)),
                (null, null), // Separator for deletion
                ((string)language["CoCle"], (_, e) => ClearStep(element, e)),
                ((string)language["CoDel"], (_, e) => DeleteStep(element, e))
            ];
            QuickContextMenu.Show(menuItems);
        }
    }
}