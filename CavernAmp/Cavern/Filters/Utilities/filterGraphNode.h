#ifndef FILTERGRAPHNODE_H
#define FILTERGRAPHNODE_H

#include <algorithm>
#include <memory>
#include <vector>

#include "../../../export.h"
#include "../filter.h"

/// \brief Wraps a Filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
class DLL_EXPORT FilterGraphNode {
private:
    std::vector<FilterGraphNode*> parents;
    std::vector<FilterGraphNode*> children;

public:
    /// Filters that add their results together before being processed by this filter and going forward in the filter graph.
    const std::vector<FilterGraphNode*>& GetParents() const { return parents; }

    /// Filters that take the result of this filter as one of their inputs.
    const std::vector<FilterGraphNode*>& GetChildren() const { return children; }

    /// The wrapped filter.
    Filter* pFilter;

    /// Wraps a filter to be handled in a multichannel complex filter set, such as equalizer platform configuration files.
    /// \param filter The wrapped filter
    FilterGraphNode(Filter* filter);

    /// Copy constructor.
    FilterGraphNode(const FilterGraphNode& other);

    /// Destructor.
    ~FilterGraphNode();

    /// Place a new node between this and the parents.
    void AddAfterParents(FilterGraphNode* newParent);

    /// Place a filter between this and the parents, then return the new node containing that filter.
    FilterGraphNode* AddAfterParents(Filter* filter);

    /// Place a new node between this and the children.
    void AddBeforeChildren(FilterGraphNode* newChild);

    /// Place a filter between this and the children, then return the new node containing that filter.
    FilterGraphNode* AddBeforeChildren(Filter* filter);

    /// Append a node to process this filter's result in the filter graph.
    void AddChild(FilterGraphNode* child);

    /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
    FilterGraphNode* AddChild(Filter* filter);

    /// Append multiple nodes to process this filter's result in the filter graph.
    void AddChildren(const std::vector<FilterGraphNode*>& addedChildren);

    /// Append this node to process a new parent's result too in the filter graph.
    void AddParent(FilterGraphNode* parent);

    /// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
    FilterGraphNode* AddParent(Filter* filter);

    /// Remove the connection of this node from the child.
    /// \param child The child to remove
    /// \param mergeConnections Connect the children of the removed child to this node
    void DetachChild(FilterGraphNode* child, bool mergeConnections);

    /// Remove the connection of this node from all children.
    void DetachChildren();

    /// Remove the connection of this node from all parents.
    void DetachParents();

    /// Remove this node from the filter graph, from both parents and children.
    /// \param mergeConnections Connect the parents and children together
    void DetachFromGraph(bool mergeConnections = true);

    /// Change ownership of two nodes' children.
    void SwapChildren(FilterGraphNode* with);
};

#ifdef __cplusplus
extern "C" {
#endif

/// Get the number of parent nodes.
int DLL_EXPORT FilterGraphNode_GetParentCount(FilterGraphNode* node);
/// Fill an array with parent node pointers.
void DLL_EXPORT FilterGraphNode_GetParents(FilterGraphNode* node, FilterGraphNode** outArray, int count);
/// Get the number of child nodes.
int DLL_EXPORT FilterGraphNode_GetChildCount(FilterGraphNode* node);
/// Fill an array with child node pointers.
void DLL_EXPORT FilterGraphNode_GetChildren(FilterGraphNode* node, FilterGraphNode** outArray, int count);
/// Create a FilterGraphNode wrapping the given filter.
FilterGraphNode* DLL_EXPORT FilterGraphNode_Create(Filter* filter);
/// Create a copy of this node and its filter. Does not copy graph relationships.
FilterGraphNode* DLL_EXPORT FilterGraphNode_Clone(FilterGraphNode* node);
/// Dispose a FilterGraphNode.
void DLL_EXPORT FilterGraphNode_Dispose(FilterGraphNode* node);
/// Place a new node between this and the parents.
void DLL_EXPORT FilterGraphNode_AddAfterParents(FilterGraphNode* node, FilterGraphNode* newParent);
/// Place a filter between this and the parents, then return the new node containing that filter.
FilterGraphNode* DLL_EXPORT FilterGraphNode_AddAfterParentsFilter(FilterGraphNode* node, Filter* filter);
/// Place a new node between this and the children.
void DLL_EXPORT FilterGraphNode_AddBeforeChildren(FilterGraphNode* node, FilterGraphNode* newChild);
/// Place a filter between this and the children, then return the new node containing that filter.
FilterGraphNode* DLL_EXPORT FilterGraphNode_AddBeforeChildrenFilter(FilterGraphNode* node, Filter* filter);
/// Append a node to process this filter's result in the filter graph.
void DLL_EXPORT FilterGraphNode_AddChild(FilterGraphNode* node, FilterGraphNode* child);
/// Append a filter to process this filter's result in the filter graph and return the new node containing that filter.
FilterGraphNode* DLL_EXPORT FilterGraphNode_AddChildFilter(FilterGraphNode* node, Filter* filter);
/// Append multiple nodes to process this filter's result in the filter graph.
void DLL_EXPORT FilterGraphNode_AddChildren(FilterGraphNode* node, FilterGraphNode** children, int count);
/// Append this node to process a new parent's result too in the filter graph.
void DLL_EXPORT FilterGraphNode_AddParent(FilterGraphNode* node, FilterGraphNode* parent);
/// Append a filter as a parent and return the new node containing that filter.
FilterGraphNode* DLL_EXPORT FilterGraphNode_AddParentFilter(FilterGraphNode* node, Filter* filter);
/// Remove the connection of this node from the child.
void DLL_EXPORT FilterGraphNode_DetachChild(FilterGraphNode* node, FilterGraphNode* child, bool mergeConnections);
/// Remove the connection of this node from all children.
void DLL_EXPORT FilterGraphNode_DetachChildren(FilterGraphNode* node);
/// Remove the connection of this node from all parents.
void DLL_EXPORT FilterGraphNode_DetachParents(FilterGraphNode* node);
/// Remove this node from the filter graph, from both parents and children.
void DLL_EXPORT FilterGraphNode_DetachFromGraph(FilterGraphNode* node, bool mergeConnections);
/// Change ownership of two nodes' children.
void DLL_EXPORT FilterGraphNode_SwapChildren(FilterGraphNode* node, FilterGraphNode* with);

#ifdef __cplusplus
}
#endif

#endif // FILTERGRAPHNODE_H
