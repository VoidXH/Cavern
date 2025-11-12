using System;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;

namespace Cavern.Format.ConfigurationFile {
    // Split point handling of ConfigurationFiles
    partial class ConfigurationFile {

        /// <summary>
        /// Add a new split with a custom <paramref name="name"/> at a specific <paramref name="index"/> of <see cref="SplitPoints"/>.
        /// </summary>
        public void AddSplitPoint(int index, string name) {
            if (index != SplitPoints.Count) {
                FilterGraphNode[] start = (FilterGraphNode[])splitPoints[index].roots.Clone();
                for (int i = 0; i < start.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)start[i].Filter).Channel;
                    start[i] = start[i].AddAfterParents(new OutputChannel(channel)).AddAfterParents(new InputChannel(channel));
                    if (index == 0) {
                        InputChannels[i] = (InputChannels[i].name, start[i]);
                    }
                }
                splitPoints.Insert(index, (name, start));
            } else {
                CreateNewSplitPoint(name);
                FilterGraphNode[] end = SplitPoints[^1].roots;
                for (int i = 0; i < end.Length; i++) {
                    ReferenceChannel channel = ((InputChannel)end[i].Filter).Channel;
                    end[i].AddChild(new OutputChannel(channel));
                }
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
        /// Adds an entry to the <see cref="SplitPoints"/> with the current state of the configuration (to the end), creating new
        /// <see cref="InputChannel"/>s after each existing <see cref="OutputChannel"/>.
        /// </summary>
        /// <remarks>If you keep track of your currently handled output nodes, set them to their children,
        /// because new input nodes are created in this function.</remarks>
        protected void CreateNewSplitPoint(string name) {
            FilterGraphNode[] nodes = InputChannels.Select(x => x.root).MapGraph()
                .Where(x => x.Filter is OutputChannel && x.Children.Count == 0).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = nodes[i].AddChild(new InputChannel(((OutputChannel)nodes[i].Filter).Channel));
            }
            splitPoints.Add((name, nodes));
        }

        /// <summary>
        /// Get the node for a split point's (referenced with an <paramref name="index"/>) given <paramref name="channel"/>.
        /// </summary>
        public FilterGraphNode GetSplitPointRoot(int index, ReferenceChannel channel) {
            FilterGraphNode[] roots = SplitPoints[index].roots;
            for (int i = 0; i < roots.Length; i++) {
                if (((InputChannel)roots[i].Filter).Channel == channel) {
                    return roots[i];
                }
            }
            throw new InvalidChannelException(new[] { channel });
        }

        /// <summary>
        /// Merge the filters of the split point at the given <paramref name="index"/> and the next one.
        /// </summary>
        public void MergeSplitPointWithNext(int index) {
            FilterGraphNode[] roots = SplitPoints[index + 1].roots;
            for (int j = 0; j < roots.Length; j++) {
                roots[j].Parents[0].DetachFromGraph(); // Output of the split at the index
                roots[j].DetachFromGraph(); // Input of the split at index + 1
            }
            splitPoints.RemoveAt(index + 1);
        }

        /// <summary>
        /// Remove any splits, leave only one continuous graph of filters.
        /// </summary>
        public void MergeSplitPoints() {
            int c = SplitPoints.Count;
            if (c <= 1) {
                return;
            }

            for (int i = 1; i < c; i++) {
                FilterGraphNode[] roots = SplitPoints[i].roots;
                for (int j = 0; j < roots.Length; j++) {
                    roots[j].Parents[0].DetachFromGraph(); // Output of the previous split
                    roots[j].DetachFromGraph(); // Input of the current split
                }
            }
            splitPoints.RemoveRange(1, SplitPoints.Count - 1);
        }

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
                splitPoints[index + 1] = (SplitPoints[index + 1].name, roots);
            }
            splitPoints.RemoveAt(index);
        }

        /// <summary>
        /// Change the <paramref name="name"/> of an existing split point at a given <paramref name="index"/>.
        /// </summary>
        public void RenameSplitPoint(int index, string name) => splitPoints[index] = (name, splitPoints[index].roots);
    }
}
