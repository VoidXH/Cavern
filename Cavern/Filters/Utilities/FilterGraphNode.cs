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
        public Filter Filter { get; }

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
        public FilterGraphNode(Filter filter) {
            Filter = filter;
        }

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
        /// Remove this node from the filter graph, including both parents and children.
        /// </summary>
        public void DetachFromGraph() {
            DetachChildren();
            DetachParents();
        }
    }
}