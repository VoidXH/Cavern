using System;
using System.Runtime.InteropServices;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Wrapper for CavernAmp's implementation of <see cref="FilterGraphNode"/>.
    /// </summary>
    public partial class FilterGraphNodeAmp : IFilterGraphNode, IDisposable {
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
        /// Get the number of parent nodes.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern int FilterGraphNode_GetParentCount(IntPtr node);

        /// <summary>
        /// Fill an array with parent node pointers.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_GetParents(IntPtr node, IntPtr[] outArray, int count);

        /// <summary>
        /// Get the number of child nodes.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern int FilterGraphNode_GetChildCount(IntPtr node);

        /// <summary>
        /// Fill an array with child node pointers.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_GetChildren(IntPtr node, IntPtr[] outArray, int count);

        /// <summary>
        /// Create a FilterGraphNode wrapping the given filter.
        /// </summary>
        [DllImport("CavernAmp.dll")]
        static extern void FilterGraphNode_Dispose(IntPtr node);
    }
}
