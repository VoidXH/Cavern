using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cavern.Filters.Utilities {
    // Map the local functions to IFilterGraphNode interface functions. Local functions are hard-typed to FilterGraphNode, so lists can be edited.
    public partial class FilterGraphNode {
        /// <inheritdoc/>
        IReadOnlyList<IFilterGraphNode> IFilterGraphNode.Parents => Parents;

        /// <inheritdoc/>
        IReadOnlyList<IFilterGraphNode> IFilterGraphNode.Children => Children;

        /// <summary>
        /// Checks if a given <paramref name="node"/> is compatible with the operations of this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void TypeCheck(IFilterGraphNode node) {
            if (!(node is FilterGraphNode)) {
                throw new InvalidOperationException("This operation only supports FilterGraphNode instances.");
            }
        }

        /// <summary>
        /// Checks if a given set of <paramref name="nodes"/> are compatible with the operations of this instance.
        /// </summary>
        static void TypeCheck(IEnumerable<IFilterGraphNode> nodes) {
            foreach (IFilterGraphNode node in nodes) {
                TypeCheck(node);
            }
        }

        /// <inheritdoc/>
        public void AddAfterParents(IFilterGraphNode newParent) {
            TypeCheck(newParent);
            AddAfterParents((FilterGraphNode)newParent);
        }

        /// <inheritdoc/>
        public void AddBeforeChildren(IFilterGraphNode newChild) {
            TypeCheck(newChild);
            AddBeforeChildren((FilterGraphNode)newChild);
        }

        /// <inheritdoc/>
        public void AddChild(IFilterGraphNode child) {
            TypeCheck(child);
            AddChild((FilterGraphNode)child);
        }

        /// <inheritdoc/>
        public void AddChildren(IEnumerable<IFilterGraphNode> addedChildren) {
            TypeCheck(addedChildren);
            AddChildren((IEnumerable<FilterGraphNode>)addedChildren);
        }

        /// <inheritdoc/>
        public void AddParent(IFilterGraphNode parent) {
            TypeCheck(parent);
            AddParent((FilterGraphNode)parent);
        }

        /// <inheritdoc/>
        public void DetachChild(IFilterGraphNode child, bool mergeConnections) {
            TypeCheck(child);
            DetachChild((FilterGraphNode)child, mergeConnections);
        }

        /// <inheritdoc/>
        public void SwapChildren(IFilterGraphNode with) {
            TypeCheck(with);
            SwapChildren((FilterGraphNode)with);
        }
    }
}
