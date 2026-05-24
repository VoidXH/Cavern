using System;
using System.Collections.Generic;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wrapper for CavernAmp's implementation of <see cref="FilterGraphNode"/>.
    /// </summary>
    public partial class FilterGraphNodeAmp : IFilterGraphNode {
        /// <summary>
        /// Delegate for a native function that gets the count of parent or child nodes of a given node.
        /// </summary>
        delegate int CountGetter(IntPtr handle);

        /// <summary>
        /// Delegate for a native function that fills an array with the pointers of parent or child nodes of a given node.
        /// </summary>
        delegate void ItemsGetter(IntPtr handle, IntPtr[] pointers, int count);

        /// <inheritdoc/>
        public IReadOnlyList<IFilterGraphNode> Parents => BuildList(FilterGraphNode_GetParentCount, FilterGraphNode_GetParents);

        /// <inheritdoc/>
        public IReadOnlyList<IFilterGraphNode> Children => BuildList(FilterGraphNode_GetChildCount, FilterGraphNode_GetChildren);

        /// <summary>
        /// Reference to the native node instance.
        /// </summary>
        readonly IntPtr handle;

        /// <summary>
        /// The <see cref="handle"/> was freed.
        /// </summary>
        bool disposed;

        /// <summary>
        /// Wraps a CavernAmp filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
        /// </summary>
        /// <param name="filter">The wrapped filter</param>
        public FilterGraphNodeAmp(FilterAmp filter) {
            handle = FilterGraphNode_Create(filter.Handle);
            filter.SetWrapped();
        }

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

        /// <inheritdoc/>
        public Filter Filter {
            get {
                IntPtr filterHandle = FilterGraphNode_GetFilter(handle);
                FilterAmp result = new FilterAmp(filterHandle);
                result.SetWrapped(); // This instance wraps it, don't dispose
                return result;
            }
            set {
                TypeCheck(value);
                FilterGraphNode_SetFilter(handle, ((FilterAmp)value).Handle);
            }
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
            List<IntPtr> children = new List<IntPtr>();
            foreach (IFilterGraphNode child in addedChildren) {
                TypeCheck(child);
                children.Add(((FilterGraphNodeAmp)child).handle);
            }
            FilterGraphNode_AddChildren(handle, children.ToArray(), children.Count);
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
        public void DetachParents() => FilterGraphNode_DetachParents(handle);

        /// <inheritdoc/>
        public void DetachChildren() => FilterGraphNode_DetachChildren(handle);

        /// <inheritdoc/>
        public void DetachFromGraph() => FilterGraphNode_DetachFromGraph(handle, true);

        /// <inheritdoc/>
        public void DetachFromGraph(bool mergeConnections) => FilterGraphNode_DetachFromGraph(handle, mergeConnections);

        /// <inheritdoc/>
        public void SwapChildren(IFilterGraphNode with) {
            TypeCheck(with);
            FilterGraphNode_SwapChildren(handle, ((FilterGraphNodeAmp)with).handle);
        }

        /// <inheritdoc/>
        public object Clone() => new FilterGraphNodeAmp(FilterGraphNode_Clone(handle));

        /// <inheritdoc/>
        public bool Equals(IFilterGraphNode other) => other is FilterGraphNodeAmp n && Filter.Equals(n.Filter);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is FilterGraphNodeAmp n && Filter.Equals(n.Filter);

        /// <inheritdoc/>
        public override int GetHashCode() => handle.GetHashCode();

        /// <inheritdoc/>
        public void Dispose() {
            if (!disposed) {
                FilterGraphNode_Dispose(handle);
                disposed = true;
            }
        }

        /// <summary>
        /// Free up native resources when the object wasn't disposed.
        /// </summary>
        ~FilterGraphNodeAmp() => Dispose();

        /// <summary>
        /// Get <see cref="IFilterGraphNode"/>s from a native node instance, using the provided delegates to get the count and pointers of the nodes.
        /// </summary>
        IReadOnlyList<IFilterGraphNode> BuildList(CountGetter getCount, ItemsGetter getItems) {
            int count = getCount(handle);
            if (count == 0) {
                return Array.Empty<IFilterGraphNode>();
            }

            IntPtr[] pointers = new IntPtr[count];
            getItems(handle, pointers, count);
            IFilterGraphNode[] nodes = new IFilterGraphNode[count];
            for (int i = 0; i < count; i++) {
                nodes[i] = new FilterGraphNodeAmp(pointers[i]);
            }
            return Array.AsReadOnly(nodes);
        }
    }
}
