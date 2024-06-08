using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Full parsed setup of a freely configurable system-wide equalizer or audio processor software.
    /// </summary>
    public abstract class ConfigurationFile {
        /// <summary>
        /// Root nodes of each channel, start attaching their filters as a children chain.
        /// </summary>
        public (string name, FilterGraphNode root)[] InputChannels { get; }

        /// <summary>
        /// Named points where the configuration file can be separated to new sections. Split points only consist of input nodes after the
        /// previous split point's output nodes.
        /// </summary>
        public IReadOnlyList<(string name, FilterGraphNode[] roots)> SplitPoints { get; }

        /// <summary>
        /// Create an empty configuration file with the passed input channels.
        /// </summary>
        protected ConfigurationFile(string name, ReferenceChannel[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i].GetShortName(), new FilterGraphNode(new InputChannel(inputs[i])));
            }

            SplitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Create an empty configuration file with the passed input channel names/labels.
        /// </summary>
        protected ConfigurationFile(string name, string[] inputs) {
            InputChannels = new (string name, FilterGraphNode root)[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) {
                InputChannels[i] = (inputs[i], new FilterGraphNode(new InputChannel(inputs[i])));
            }

            SplitPoints = new List<(string, FilterGraphNode[])> {
                (name, InputChannels.GetItem2s())
            };
        }

        /// <summary>
        /// Add a new split with a custom <paramref name="name"/> at a specific <paramref name="index"/> of <see cref="SplitPoints"/>.
        /// </summary>
        public void AddSplitPoint(int index, string name) {
            if (index != SplitPoints.Count) {
                List<(string name, FilterGraphNode[] roots)> splits = (List<(string, FilterGraphNode[])>)SplitPoints;
                FilterGraphNode[] start = (FilterGraphNode[])splits[index].roots.Clone();
                for (int i = 0; i < start.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)start[i].Filter).Channel;
                    start[i] = start[i].AddAfterParents(new OutputChannel(channel)).AddAfterParents(new InputChannel(channel));
                }
                splits.Insert(index, (name, start));
            } else {
                CreateNewSplitPoint(name);
                FilterGraphNode[] end = SplitPoints[^1].roots;
                for (int i = 0; i < end.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)end[i].Filter).Channel;
                    end[i].AddChild(new OutputChannel(channel));
                }
                return;
            }
        }

        /// <summary>
        /// Clears all filters in one of the <see cref="SplitPoints"/> by <paramref name="index"/>.
        /// </summary>
        public void ClearSplitPoint(int index) {
            if (index == SplitPoints.Count - 1) { // Last split can be cleared and replaced with new outputs
                FilterGraphNode[] roots = SplitPoints[index].roots;
                for (int i = 0; i < roots.Length; i++) {
                    roots[i].DetachChildren();
                    roots[i].AddChild(new OutputChannel(((InputChannel)roots[i].Filter).Channel));
                }
            } else { // General case: clear the children and use the next split to fetch the outputs
                FilterGraphNode[] roots = SplitPoints[index].roots,
                    next = SplitPoints[index + 1].roots;
                for (int i = 0; i < roots.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)roots[i].Filter).Channel;
                    FilterGraphNode equivalent = next.First(x => ((InputChannel)x.Filter).Channel == channel);

                    roots[i].DetachChildren();
                    equivalent.Parents[0].DetachParents();
                    roots[i].AddChild(equivalent.Parents[0]);
                }
            }
        }

        /// <summary>
        /// Clears all filters in one of the <see cref="SplitPoints"/> by <paramref name="name"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        public void ClearSplitPoint(string name) => ClearSplitPoint(GetSplitPointIndexByName(name));

        /// <summary>
        /// Removes one of the <see cref="SplitPoints"/> by <paramref name="index"/> and clears all the filters it contains.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">The last split point can't be removed. To bypass this restriction,
        /// you could add an empty split point and remove the previous last one.</exception>
        public void RemoveSplitPoint(int index) {
            if (SplitPoints.Count == 1) {
                throw new IndexOutOfRangeException();
            }

            if (index == SplitPoints.Count - 1) { // Last split can be just removed
                FilterGraphNode[] roots = SplitPoints[index].roots;
                for (int i = 0; i < roots.Length; i++) {
                    roots[i].DetachFromGraph(false);
                }
            } else { // General case: transfer children from the next set of roots, then swap roots
                FilterGraphNode[] roots = SplitPoints[index].roots,
                    next = SplitPoints[index + 1].roots;
                for (int i = 0; i < roots.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)roots[i].Filter).Channel;
                    FilterGraphNode equivalent = next.First(x => ((InputChannel)x.Filter).Channel == channel);

                    roots[i].DetachChildren();
                    FilterGraphNode[] oldChildren = equivalent.Children.ToArray();
                    equivalent.DetachChildren();
                    roots[i].AddChildren(oldChildren);
                }
                ((List<(string, FilterGraphNode[])>)SplitPoints)[index + 1] = (SplitPoints[index + 1].name, roots);
            }
            ((List<(string, FilterGraphNode[])>)SplitPoints).RemoveAt(index);
        }

        /// <summary>
        /// Removes one of the <see cref="SplitPoints"/> by <paramref name="name"/> and clears all the filters it contains.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        /// <exception cref="IndexOutOfRangeException">The last split point can't be removed. To bypass this restriction,
        /// you could add an empty split point and remove the previous last one.</exception>
        public void RemoveSplitPoint(string name) => RemoveSplitPoint(GetSplitPointIndexByName(name));

        /// <summary>
        /// Adds an entry to the <see cref="SplitPoints"/> with the current state of the configuration, creating new
        /// <see cref="InputChannel"/>s after each existing <see cref="OutputChannel"/>.
        /// </summary>
        /// <remarks>If you keep track of your currently handled output nodes, set them to their children,
        /// because new input nodes are created in this function.</remarks>
        protected void CreateNewSplitPoint(string name) {
            FilterGraphNode[] nodes =
                FilterGraphNodeUtils.MapGraph(InputChannels.Select(x => x.root))
                .Where(x => x.Filter is OutputChannel && x.Children.Count == 0).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = nodes[i].AddChild(new InputChannel(((OutputChannel)nodes[i].Filter).Channel));
            }
            ((List<(string, FilterGraphNode[])>)SplitPoints).Add((name, nodes));
        }

        /// <summary>
        /// Remove as many merge nodes (null filters) as possible.
        /// </summary>
        protected void Optimize() {
            for (int i = 0; i < InputChannels.Length; i++) {
                IReadOnlyList<FilterGraphNode> children = InputChannels[i].root.Children;
                for (int j = 0, c = children.Count; j < c;) {
                    if (!Optimize(children[j])) {
                        j++;
                    }
                }
            }
        }

        /// <summary>
        /// Recursive part of <see cref="Optimize()"/>.
        /// </summary>
        /// <returns>Optimization was done and the children of the passed <paramref name="node"/> was modified.
        /// This means the currently processed element was removed and new were added, so the loop counter shouldn't increase
        /// in the iteration where this function was called from.</returns>
        bool Optimize(FilterGraphNode node) {
            bool optimized = false;
            if (node.Filter == null) {
                node.DetachFromGraph();
                optimized = true;
            }

            IReadOnlyList<FilterGraphNode> children = node.Children;
            for (int i = 0, c = children.Count; i < c; i++) {
                if (Optimize(children[i])) {
                    optimized = true;
                    i--;
                }
            }
            return optimized;
        }

        /// <summary>
        /// Get the index in the <see cref="SplitPoints"/> list by a split point's <paramref name="name"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="name"/> was not found
        /// among the <see cref="SplitPoints"/></exception>
        int GetSplitPointIndexByName(string name) {
            for (int i = 0, c = SplitPoints.Count; i < c; i++) {
                if (SplitPoints[i].name == name) {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(name));
        }
    }
}