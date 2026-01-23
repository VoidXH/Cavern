using Microsoft.Msagl.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Format.ConfigurationFile.Presets;
using Cavern.Format.FilterSet;
using Cavern.WPF;
using VoidX.WPF;

using FilterStudio.Graphs;
using FilterStudio.Windows;
using FilterStudio.Windows.PipelineSteps;
using Microsoft.Win32;

namespace FilterStudio {
    // Handlers of the pipeline graph control
    partial class MainWindow {
        /// <summary>
        /// Add a new empty pipeline step after the selected node.
        /// </summary>
        void AddStep(object sender, RoutedEventArgs e) => PipelineAddition(sender, uid => {
            pipeline.Source.AddSplitPoint(uid, (string)language["NSNew"]);
            return true;
        });

        /// <summary>
        /// Add a new crossover step after the selected node.
        /// </summary>
        void AddCrossover(object sender, RoutedEventArgs e) => PipelineAddition(sender, uid => {
            CrossoverDialog dialog = new(GetChannels());
            if (!dialog.ShowDialog().Value) {
                return false;
            }

            CrossoverFilterSet crossover = new((string)language["LabXO"], dialog.Type, SampleRate, 65536, dialog.Description);
            crossover.Add(pipeline.Source, uid);
            return true;
        });

        /// <summary>
        /// Makes a new configuration file that only contains the selected pipeline step.
        /// </summary>
        void CreateNewConfigurationFromStep(object sender, RoutedEventArgs e) => PipelineAction(sender, uid =>
            pipeline.Source = new CavernFilterStudioConfigurationFile(pipeline.Source.SplitPoints[uid])
        );

        /// <summary>
        /// Export the currently selected pipeline step as a <see cref="FilterSet"/>.
        /// </summary>
        void ExportStepToDevice(object sender, RoutedEventArgs e) => PipelineAction(sender, uid => {
            FilterSetTargetSelector target = new() {
                Background = Background,
                Resources = Resources,
            };
            if (!target.ShowDialog().Value) {
                return;
            }

            FilterSet set = FilterSet.Create(target.Result, GetChannels(), SampleRate);
            SaveFileDialog exporter = new() {
                Filter = $"{set.FileExtension.ToUpper(CultureInfo.InvariantCulture)} {language["files"]}|*.{set.FileExtension}",
                FileName = (string)language["ExpFN"],
            };
            if (!exporter.ShowDialog().Value) {
                return;
            }

            List<Filter>[] filters = new List<Filter>[set.ChannelCount];
            for (int ch = 0; ch < set.ChannelCount; ch++) {
                filters[ch] = [];
                FilterGraphNode current = pipeline.Source.GetSplitPointRoot(uid, ch);
                do {
                    current = current.Children[0];
                    filters[ch].Add(current.Filter);
                } while (current.Children.Count == 1 && current.Filter is not OutputChannel);
            }

            if (set is IIRFilterSet iirSet) {
                for (int i = 0; i < set.ChannelCount; i++) {
                    iirSet.SetupChannel(i, [.. filters[i].OfType<BiquadFilter>()]);
                }
            }
            set.Export(exporter.FileName);
        });

        /// <summary>
        /// Merge the selected pipeline step with the next.
        /// </summary>
        void MergeWithNext(object sender, RoutedEventArgs e) => PipelineAction(sender, uid => {
            if (uid < pipeline.Source.SplitPoints.Count - 1) {
                pipeline.Source.MergeSplitPointWithNext(uid);
            } else {
                Error((string)language["NLaMe"]);
            }
        });

        /// <summary>
        /// Merge all pipeline steps to one.
        /// </summary>
        void MergeSteps(object _, RoutedEventArgs e) {
            if (pipeline.Source == null) {
                return;
            }
            pipeline.Source.MergeSplitPoints();
            Reload(null, null);
        }

        /// <summary>
        /// Rename the currently selected pipeline step through a popup.
        /// </summary>
        void RenameStep(object sender, RoutedEventArgs e) => PipelineAction(sender, uid => {
            RenameDialog rename = new RenameDialog(pipeline.Source.SplitPoints[uid].Name);
            if (rename.ShowDialog().Value) {
                pipeline.Source.RenameSplitPoint(uid, rename.NewName);
            }
        });

        /// <summary>
        /// Clear the currently selected pipeline step (remove all its filters).
        /// </summary>
        void ClearStep(object sender, RoutedEventArgs e) => PipelineAction(sender, pipeline.Source.ClearSplitPoint);

        /// <summary>
        /// Delete the currently selected step from the pipeline.
        /// </summary>
        void DeleteStep(object sender, RoutedEventArgs e) => PipelineAction(sender, uid => {
            try {
                pipeline.Source.RemoveSplitPoint(uid);
            } catch (IndexOutOfRangeException) {
                Error((string)language["NLaSP"]);
            }
        });

        /// <summary>
        /// Perform an action on a pipeline step, then reload both the pipeline and the graph.
        /// </summary>
        /// <param name="sender">Either the selection from the graph or the menu item initializing the operation</param>
        /// <param name="action">What should happen with the pipeline step by its uid</param>
        void PipelineAction(object sender, Action<int> action) {
            StyledNode node = pipeline.GetSelectedNode(sender);
            if (node == null) {
                Error((string)language["NPNod"]);
            } else if (int.TryParse(node.Id, out int uid)) {
                action(uid);
                Reload(null, null);
                pipeline.SelectNode(uid.ToString()); // Reselect the previously selected node if it's still present
            } else {
                Error((string)language["NPiSi"]);
            }
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
                Reload(null, null);
                pipeline.SelectNode(uid.ToString()); // Select the new step
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
                (null, null),
                ((string)language["CoSol"], (_, e) => CreateNewConfigurationFromStep(element, e)),
                ((string)language["CoMeN"], (_, e) => MergeWithNext(element, e)),
                ((string)language["CoExp"], (_, e) => ExportStepToDevice(element, e)),
                (null, null),
                ((string)language["CoRen"], (_, e) => RenameStep(element, e)),
                ((string)language["CoCle"], (_, e) => ClearStep(element, e)),
                ((string)language["CoDel"], (_, e) => DeleteStep(element, e))
            ];
            QuickContextMenu.Show(menuItems);
        }
    }
}
