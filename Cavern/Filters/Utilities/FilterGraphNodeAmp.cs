using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wrapper for CavernAmp's implementation of <see cref="FilterGraphNode"/>.
    /// </summary>
    public class FilterGraphNodeAmp : IFilterGraphNode {
        /// <inheritdoc/>
        public IReadOnlyList<IFilterGraphNode> Parents => throw new NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IFilterGraphNode> Children => throw new NotImplementedException();

        /// <summary>
        /// Reference to the native node instance.
        /// </summary>
        IntPtr handle;

        /// <summary>
        /// Wraps a CavernAmp filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
        /// </summary>
        /// <param name="filter">The wrapped filter</param>
        public FilterGraphNodeAmp(FilterAmp filter) => handle = FilterGraphNode_Create(filter.Handle);

        /// <summary>
        /// Wraps a native node instance.
        /// </summary>
        FilterGraphNodeAmp(IntPtr handle) => this.handle = handle;

        /// <summary>
        /// Checks if a given <paramref name="node"/> is compatible with the operations of this instance.
        /// </summary>
        static void TypeCheck(IFilterGraphNode node) {
            if (!(node is FilterGraphNodeAmp)) {
                throw new InvalidOperationException("This operation only supports FilterGraphNodeAmp instances.");
            }
        }

        /// <summary>
        /// Checks if a given <paramref name="filter"/> is compatible with the operations of this instance.
        /// </summary>
        static void TypeCheck(Filter filter) {
            if (!(filter is FilterAmp)) {
                throw new InvalidOperationException("This operation only supports FilterAmp instances.");
            }
        }

        /// <summary>
        /// Create a FilterGraphNode wrapping the given filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_Create(IntPtr filter);

        /// <summary>
        /// Create a copy of this node and its filter. Does not copy graph relationships.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_Clone(IntPtr node);

        /// <summary>
        /// Place a new node between this and the parents.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_AddAfterParents(IntPtr node, IntPtr newParent);

        /// <summary>
        /// Place a filter between this and the parents, then return the new node containing that filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_AddAfterParentsFilter(IntPtr node, IntPtr filter);

        /// <summary>
        /// Place a new node between this and the children.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_AddBeforeChildren(IntPtr node, IntPtr newChild);

        /// <summary>
        /// Place a filter between this and the children, then return the new node containing that filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_AddBeforeChildrenFilter(IntPtr node, IntPtr filter);

        /// <summary>
        /// Append a node to process this filter's result in the filter graph.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_AddChild(IntPtr node, IntPtr child);

        /// <summary>
        /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_AddChildFilter(IntPtr node, IntPtr filter);

        /// <summary>
        /// Append multiple nodes to process this filter's result in the filter graph.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_AddChildren(IntPtr node, IntPtr[] children, int count);

        /// <summary>
        /// Append this node to process a new parent's result too in the filter graph.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_AddParent(IntPtr node, IntPtr parent);

        /// <summary>
        /// Append a filter as a parent and return the new node containing that filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern IntPtr FilterGraphNode_AddParentFilter(IntPtr node, IntPtr filter);

        /// <summary>
        /// Remove the connection of this node from the child.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_DetachChild(IntPtr node, IntPtr child, bool mergeConnections);

        /// <summary>
        /// Remove the connection of this node from all children.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_DetachChildren(IntPtr node);

        /// <summary>
        /// Remove the connection of this node from all parents.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_DetachParents(IntPtr node);

        /// <summary>
        /// Remove this node from the filter graph, from both parents and children.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_DetachFromGraph(IntPtr node, bool mergeConnections);

        /// <summary>
        /// Change ownership of two nodes' children.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_SwapChildren(IntPtr node, IntPtr with);

        /// <summary>
        /// Create a FilterGraphNode wrapping the given filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_Dispose(IntPtr node);

        /// <inheritdoc/>
        public Filter Filter {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void AddAfterParents(IFilterGraphNode newParent) {
            TypeCheck(newParent);
            FilterGraphNode_AddAfterParents(handle, ((FilterGraphNodeAmp)newParent).handle);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddAfterParents(Filter filter) {
            TypeCheck(filter);
            return new FilterGraphNodeAmp(FilterGraphNode_AddAfterParentsFilter(handle, ((FilterAmp)filter).Handle));
        }

        /// <inheritdoc/>
        public void AddBeforeChildren(IFilterGraphNode newChild) {
            TypeCheck(newChild);
            FilterGraphNode_AddBeforeChildren(handle, ((FilterGraphNodeAmp)newChild).handle);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddBeforeChildren(Filter filter) {
            TypeCheck(filter);
            return new FilterGraphNodeAmp(FilterGraphNode_AddBeforeChildrenFilter(handle, ((FilterAmp)filter).Handle));
        }

        /// <inheritdoc/>
        public void AddChild(IFilterGraphNode child) {
            TypeCheck(child);
            FilterGraphNode_AddChild(handle, ((FilterGraphNodeAmp)child).handle);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddChild(Filter filter) {
            TypeCheck(filter);
            return new FilterGraphNodeAmp(FilterGraphNode_AddChildFilter(handle, ((FilterAmp)filter).Handle));
        }

        /// <inheritdoc/>
        public void AddChildren(IEnumerable<IFilterGraphNode> addedChildren) {
            var childrenArray = new List<IntPtr>();
            foreach (var child in addedChildren) {
                TypeCheck(child);
                childrenArray.Add(((FilterGraphNodeAmp)child).handle);
            }
            FilterGraphNode_AddChildren(handle, childrenArray.ToArray(), childrenArray.Count);
        }

        /// <inheritdoc/>
        public void AddParent(IFilterGraphNode parent) {
            TypeCheck(parent);
            FilterGraphNode_AddParent(handle, ((FilterGraphNodeAmp)parent).handle);
        }

        /// <inheritdoc/>
        public IFilterGraphNode AddParent(Filter filter) {
            TypeCheck(filter);
            return new FilterGraphNodeAmp(FilterGraphNode_AddParentFilter(handle, ((FilterAmp)filter).Handle));
        }

        /// <inheritdoc/>
        public void DetachChild(IFilterGraphNode child, bool mergeConnections) {
            TypeCheck(child);
            FilterGraphNode_DetachChild(handle, ((FilterGraphNodeAmp)child).handle, mergeConnections);
        }

        /// <inheritdoc/>
        public void DetachParents() {
            FilterGraphNode_DetachParents(handle);
        }

        /// <inheritdoc/>
        public void DetachChildren() {
            FilterGraphNode_DetachChildren(handle);
        }

        /// <inheritdoc/>
        public void DetachFromGraph() {
            FilterGraphNode_DetachFromGraph(handle, true);
        }

        /// <inheritdoc/>
        public void DetachFromGraph(bool mergeConnections) {
            FilterGraphNode_DetachFromGraph(handle, mergeConnections);
        }

        /// <inheritdoc/>
        public void SwapChildren(IFilterGraphNode with) {
            TypeCheck(with);
            FilterGraphNode_SwapChildren(handle, ((FilterGraphNodeAmp)with).handle);
        }

        /// <inheritdoc/>
        public object Clone() => new FilterGraphNodeAmp(FilterGraphNode_Clone(handle));

        /// <inheritdoc/>
        public void Dispose() {
            if (handle != IntPtr.Zero) {
                FilterGraphNode_Dispose(handle);
                handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Free up native resources when the object wasn't disposed.
        /// </summary>
        ~FilterGraphNodeAmp() => Dispose();
    }
}
