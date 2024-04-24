using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wraps a filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
    /// </summary>
    public class FilterGraphNode {
        /// <summary>
        /// Filters that add their results together before being processed by this filter and going forward in the filter graph.
        /// </summary>
        public IReadOnlyList<FilterGraphNode> Parents => parents;

        /// <summary>
        /// Filters that take the result of this filter as one of their inputs.
        /// </summary>
        public IReadOnlyList<FilterGraphNode> Children => children;

        /// <summary>
        /// The wrapped filter.
        /// </summary>
        public Filter Filter { get; set; }

        /// <summary>
        /// Filters that add their results together before being processed by this filter and going forward in the filter graph.
        /// </summary>
        readonly List<FilterGraphNode> parents = new List<FilterGraphNode>();

        /// <summary>
        /// Filters that take the result of this filter as one of their inputs.
        /// </summary>
        readonly List<FilterGraphNode> children = new List<FilterGraphNode>();

        /// <summary>
        /// Wraps a filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
        /// </summary>
        /// <param name="filter">The wrapped filter</param>
        public FilterGraphNode(Filter filter) => Filter = filter;

        /// <summary>
        /// Append a node to process this filter's result in the filter graph.
        /// </summary>
        public void AddChild(FilterGraphNode child) {
            children.Add(child);
            child.parents.Add(this);
        }

        /// <summary>
        /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
        /// </summary>
        public FilterGraphNode AddChild(Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            children.Add(node);
            node.parents.Add(this);
            return node;
        }

        /// <summary>
        /// Append this node to process a new <paramref name="parent"/>'s result too in the filter graph.
        /// </summary>
        public void AddParent(FilterGraphNode parent) {
            parents.Add(parent);
            parent.children.Add(this);
        }

        /// <summary>
        /// Remove the connection of this node from the <paramref name="child"/>.
        /// </summary>
        /// <param name="child">The child to remove</param>
        /// <param name="mergeConnections">Connect the children of the removed <paramref name="child"/> to this node</param>
        public void DetachChild(FilterGraphNode child, bool mergeConnections) {
            children.Remove(child);
            child.parents.Remove(this);
            if (mergeConnections) {
                children.AddRange(child.children);
            }
        }

        /// <summary>
        /// Remove the connection of this node from all <see cref="children"/>.
        /// </summary>
        public void DetachChildren() {
            for (int i = 0, c = children.Count; i < c; i++) {
                children[i].parents.Remove(this);
            }
            children.Clear();
        }

        /// <summary>
        /// Remove the connection of this node from all <see cref="parents"/>.
        /// </summary>
        public void DetachParents() {
            for (int i = 0, c = parents.Count; i < c; i++) {
                parents[i].children.Remove(this);
            }
            parents.Clear();
        }

        /// <summary>
        /// Remove this node from the filter graph, and connect the parents and children directly.
        /// </summary>
        public void DetachFromGraph() => DetachFromGraph(true);

        /// <summary>
        /// Remove this node from the filter graph, from both parents and children.
        /// </summary>
        /// <param name="mergeConnections">Connect the parents and children together</param>
        public void DetachFromGraph(bool mergeConnections) {
            if (mergeConnections) {
                for (int i = 0, c = parents.Count; i < c; i++) {
                    for (int j = 0, c2 = children.Count; j < c2; j++) {
                        parents[i].AddChild(children[j]);
                    }
                }
            }

            DetachChildren();
            DetachParents();
        }

        /// <inheritdoc/>
        public override string ToString() => Filter != null ? Filter.ToString() : "Merge";
    }
}