using System;
using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wraps a filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
    /// </summary>
    public interface IFilterGraphNode : ICloneable, IDisposable {
        /// <summary>
        /// Filters that add their results together before being processed by this filter and going forward in the filter graph.
        /// </summary>
        IReadOnlyList<IFilterGraphNode> Parents { get; }

        /// <summary>
        /// Filters that take the result of this filter as one of their inputs.
        /// </summary>
        IReadOnlyList<IFilterGraphNode> Children { get; }

        /// <summary>
        /// The wrapped filter.
        /// </summary>
        Filter Filter { get; set; }

        /// <summary>
        /// Place a <see cref="FilterGraphNode"/> between this and the <see cref="Parents"/>.
        /// </summary>
        void AddAfterParents(IFilterGraphNode newParent);

        /// <summary>
        /// Place a <paramref name="filter"/> between this and the <see cref="Parents"/>, then return the new node containing that filter.
        /// </summary>
        IFilterGraphNode AddAfterParents(Filter filter);

        /// <summary>
        /// Place a <see cref="FilterGraphNode"/> between this and the <see cref="Children"/>.
        /// </summary>
        void AddBeforeChildren(IFilterGraphNode newChild);

        /// <summary>
        /// Place a <paramref name="filter"/> between this and the <see cref="Children"/>, then return the new node containing that filter.
        /// </summary>
        IFilterGraphNode AddBeforeChildren(Filter filter);

        /// <summary>
        /// Append a node to process this filter's result in the filter graph.
        /// </summary>
        void AddChild(IFilterGraphNode child);

        /// <summary>
        /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
        /// </summary>
        IFilterGraphNode AddChild(Filter filter);

        /// <summary>
        /// Append multiple nodes to process this filter's result in the filter graph.
        /// </summary>
        void AddChildren(IEnumerable<IFilterGraphNode> addedChildren);

        /// <summary>
        /// Append this node to process a new <paramref name="parent"/>'s result too in the filter graph.
        /// </summary>
        void AddParent(IFilterGraphNode parent);

        /// <summary>
        /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
        /// </summary>
        IFilterGraphNode AddParent(Filter filter);

        /// <summary>
        /// Remove the connection of this node from the <paramref name="child"/>.
        /// </summary>
        /// <param name="child">The child to remove</param>
        /// <param name="mergeConnections">Connect the children of the removed <paramref name="child"/> to this node</param>
        void DetachChild(IFilterGraphNode child, bool mergeConnections);

        /// <summary>
        /// Remove the connection of this node from all <see cref="Parents"/>.
        /// </summary>
        void DetachParents();

        /// <summary>
        /// Remove the connection of this node from all <see cref="Children"/>.
        /// </summary>
        void DetachChildren();

        /// <summary>
        /// Remove this node from the filter graph, and connect the parents and children directly.
        /// </summary>
        void DetachFromGraph();

        /// <summary>
        /// Remove this node from the filter graph, from both parents and children.
        /// </summary>
        /// <param name="mergeConnections">Connect the parents and children together</param>
        void DetachFromGraph(bool mergeConnections);

        /// <summary>
        /// Change ownership of two nodes' <see cref="Children"/>.
        /// </summary>
        void SwapChildren(IFilterGraphNode with);
    }
}
