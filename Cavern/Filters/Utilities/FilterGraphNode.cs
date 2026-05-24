using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wraps a filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq} ({GetHashCode(),nq})")]
    public partial class FilterGraphNode : IFilterGraphNode {
        /// <summary>
        /// Checks if two <see cref="FilterGraphNode"/> instances wrap the same filter.
        /// </summary>
        public static bool operator ==(FilterGraphNode lhs, FilterGraphNode rhs) => ReferenceEquals(lhs, rhs) || lhs?.Filter == rhs?.Filter;

        /// <summary>
        /// Checks if two <see cref="FilterGraphNode"/> instances wrap different filters.
        /// </summary>
        public static bool operator !=(FilterGraphNode lhs, FilterGraphNode rhs) => !(lhs == rhs);

        /// <inheritdoc/>
        public IReadOnlyList<FilterGraphNode> Parents => parents;

        /// <inheritdoc/>
        public IReadOnlyList<FilterGraphNode> Children => children;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void AddAfterParents(FilterGraphNode newParent) {
            newParent.parents.AddRange(parents);
            for (int i = 0, c = parents.Count; i < c; i++) {
                parents[i].children.Remove(this);
                parents[i].children.Add(newParent);
            }
            parents.Clear();
            AddParent(newParent);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddAfterParents(Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            AddAfterParents(node);
            return node;
        }

        /// <inheritdoc/>
        public void AddBeforeChildren(FilterGraphNode newChild) {
            newChild.children.AddRange(children);
            for (int i = 0, c = children.Count; i < c; i++) {
                children[i].parents.Remove(this);
                children[i].parents.Add(newChild);
            }
            children.Clear();
            AddChild(newChild);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddBeforeChildren(Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            AddBeforeChildren(node);
            return node;
        }

        /// <inheritdoc/>
        public void AddChild(FilterGraphNode child) {
            children.Add(child);
            child.parents.Add(this);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddChild(Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            children.Add(node);
            node.parents.Add(this);
            return node;
        }

        /// <inheritdoc/>
        public void AddChildren(IEnumerable<FilterGraphNode> addedChildren) {
            children.AddRange(addedChildren);
            foreach (FilterGraphNode child in addedChildren) {
                child.parents.Add(this);
            }
        }

        /// <inheritdoc/>
        public void AddParent(FilterGraphNode parent) {
            parents.Add(parent);
            parent.children.Add(this);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddParent(Filter filter) {
            FilterGraphNode node = new FilterGraphNode(filter);
            parents.Add(node);
            node.children.Add(this);
            return node;
        }

        /// <inheritdoc/>
        public void DetachChild(FilterGraphNode child, bool mergeConnections) {
            children.Remove(child);
            child.parents.Remove(this);
            if (mergeConnections) {
                children.AddRange(child.children);
            }
        }

        /// <inheritdoc/>
        public void DetachParents() {
            for (int i = 0, c = parents.Count; i < c; i++) {
                parents[i].children.Remove(this);
            }
            parents.Clear();
        }

        /// <inheritdoc/>
        public void DetachChildren() {
            for (int i = 0, c = children.Count; i < c; i++) {
                children[i].parents.Remove(this);
            }
            children.Clear();
        }

        /// <inheritdoc/>
        public void DetachFromGraph() => DetachFromGraph(true);

        /// <inheritdoc/>
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
        public void SwapChildren(FilterGraphNode with) {
            List<FilterGraphNode> temp = new List<FilterGraphNode>();
            temp.AddRange(children);
            DetachChildren();
            AddChildren(with.children);
            with.DetachChildren();
            with.AddChildren(temp);
        }

        /// <summary>
        /// Create a copy of this node and its filter. Does not copy graph relationships.
        /// </summary>
        public object Clone() => new FilterGraphNode((Filter)Filter.Clone());

        /// <inheritdoc/>
        public void Dispose() {
            // Not needed for this implementation
        }

        /// <inheritdoc/>
        public bool Equals(IFilterGraphNode other) => other is FilterGraphNode node && Filter == node.Filter;

        /// <inheritdoc/>
        public override bool Equals(object other) => other is FilterGraphNode node && Filter == node.Filter;

        /// <inheritdoc/>
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(Filter);

        /// <inheritdoc/>
        public override string ToString() => Filter != null ?
            (Filter is ILocalizableToString loc ? loc.ToString(CultureInfo.CurrentCulture) : Filter.ToString()) :
            "Merge";
    }
}
