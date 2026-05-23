#include "filterGraphNode.h"

FilterGraphNode::FilterGraphNode(Filter* filter) : parents(), children() {
    pFilter = filter;
}

FilterGraphNode::FilterGraphNode(const FilterGraphNode& other) : parents(), children() {
    pFilter = other.pFilter != nullptr ? (Filter*)other.pFilter->Clone() : nullptr;
}

FilterGraphNode::~FilterGraphNode() {
    delete pFilter;
}

void FilterGraphNode::AddAfterParents(FilterGraphNode* newParent) {
    parents.insert(parents.end(), newParent->parents.begin(), newParent->parents.end());
    for (size_t i = 0; i < parents.size(); i++) {
        parents[i]->children.erase(
            std::remove(parents[i]->children.begin(), parents[i]->children.end(), this),
            parents[i]->children.end());
        parents[i]->children.push_back(newParent);
    }
    parents.clear();
    AddParent(newParent);
}

FilterGraphNode* FilterGraphNode::AddAfterParents(Filter* filter) {
    FilterGraphNode* node = new FilterGraphNode(filter);
    AddAfterParents(node);
    return node;
}

void FilterGraphNode::AddBeforeChildren(FilterGraphNode* newChild) {
    children.insert(children.end(), newChild->children.begin(), newChild->children.end());
    for (size_t i = 0; i < children.size(); i++) {
        children[i]->parents.erase(
            std::remove(children[i]->parents.begin(), children[i]->parents.end(), this),
            children[i]->parents.end());
        children[i]->parents.push_back(newChild);
    }
    children.clear();
    AddChild(newChild);
}

FilterGraphNode* FilterGraphNode::AddBeforeChildren(Filter* filter) {
    FilterGraphNode* node = new FilterGraphNode(filter);
    AddBeforeChildren(node);
    return node;
}

void FilterGraphNode::AddChild(FilterGraphNode* child) {
    children.push_back(child);
    child->parents.push_back(this);
}

FilterGraphNode* FilterGraphNode::AddChild(Filter* filter) {
    FilterGraphNode* node = new FilterGraphNode(filter);
    children.push_back(node);
    node->parents.push_back(this);
    return node;
}

void FilterGraphNode::AddChildren(const std::vector<FilterGraphNode*>& addedChildren) {
    children.insert(children.end(), addedChildren.begin(), addedChildren.end());
    for (size_t i = 0; i < addedChildren.size(); i++) {
        addedChildren[i]->parents.push_back(this);
    }
}

void FilterGraphNode::AddParent(FilterGraphNode* parent) {
    parents.push_back(parent);
    parent->children.push_back(this);
}

FilterGraphNode* FilterGraphNode::AddParent(Filter* filter) {
    FilterGraphNode* node = new FilterGraphNode(filter);
    parents.push_back(node);
    node->children.push_back(this);
    return node;
}

void FilterGraphNode::DetachChild(FilterGraphNode* child, bool mergeConnections) {
    children.erase(
        std::remove(children.begin(), children.end(), child),
        children.end());
    child->parents.erase(
        std::remove(child->parents.begin(), child->parents.end(), this),
        child->parents.end());
    if (mergeConnections) {
        children.insert(children.end(), child->children.begin(), child->children.end());
    }
}

void FilterGraphNode::DetachChildren() {
    for (size_t i = 0; i < children.size(); i++) {
        children[i]->parents.erase(
            std::remove(children[i]->parents.begin(), children[i]->parents.end(), this),
            children[i]->parents.end());
    }
    children.clear();
}

void FilterGraphNode::DetachParents() {
    for (size_t i = 0; i < parents.size(); i++) {
        parents[i]->children.erase(
            std::remove(parents[i]->children.begin(), parents[i]->children.end(), this),
            parents[i]->children.end());
    }
    parents.clear();
}

void FilterGraphNode::DetachFromGraph(bool mergeConnections) {
    if (mergeConnections) {
        for (size_t i = 0; i < parents.size(); i++) {
            for (size_t j = 0; j < children.size(); j++) {
                parents[i]->AddChild(children[j]);
            }
        }
    }

    DetachChildren();
    DetachParents();
}

void FilterGraphNode::SwapChildren(FilterGraphNode* with) {
    std::vector<FilterGraphNode*> temp(children);
    DetachChildren();
    AddChildren(with->children);
    with->DetachChildren();
    with->AddChildren(temp);
}

int DLL_EXPORT FilterGraphNode_GetParentCount(FilterGraphNode* node) {
    return (int)node->GetParents().size();
}

void DLL_EXPORT FilterGraphNode_GetParents(FilterGraphNode* node, FilterGraphNode** outArray, int count) {
    for (int i = 0; i < count; i++) {
        outArray[i] = node->GetParents()[i];
    }
}

int DLL_EXPORT FilterGraphNode_GetChildCount(FilterGraphNode* node) {
    return (int)node->GetChildren().size();
}

void DLL_EXPORT FilterGraphNode_GetChildren(FilterGraphNode* node, FilterGraphNode** outArray, int count) {
    for (int i = 0; i < count; i++) {
        outArray[i] = node->GetChildren()[i];
    }
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_Create(Filter* filter) {
    return new FilterGraphNode(filter);
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_Clone(FilterGraphNode* node) {
    return new FilterGraphNode(*node);
}

void DLL_EXPORT FilterGraphNode_Dispose(FilterGraphNode* node) {
    delete node;
}

void DLL_EXPORT FilterGraphNode_AddAfterParents(FilterGraphNode* node, FilterGraphNode* newParent) {
    node->AddAfterParents(newParent);
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_AddAfterParentsFilter(FilterGraphNode* node, Filter* filter) {
    return node->AddAfterParents(filter);
}

void DLL_EXPORT FilterGraphNode_AddBeforeChildren(FilterGraphNode* node, FilterGraphNode* newChild) {
    node->AddBeforeChildren(newChild);
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_AddBeforeChildrenFilter(FilterGraphNode* node, Filter* filter) {
    return node->AddBeforeChildren(filter);
}

void DLL_EXPORT FilterGraphNode_AddChild(FilterGraphNode* node, FilterGraphNode* child) {
    node->AddChild(child);
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_AddChildFilter(FilterGraphNode* node, Filter* filter) {
    return node->AddChild(filter);
}

void DLL_EXPORT FilterGraphNode_AddChildren(FilterGraphNode* node, FilterGraphNode** children, int count) {
    std::vector<FilterGraphNode*> vec(children, children + count);
    node->AddChildren(vec);
}

void DLL_EXPORT FilterGraphNode_AddParent(FilterGraphNode* node, FilterGraphNode* parent) {
    node->AddParent(parent);
}

FilterGraphNode* DLL_EXPORT FilterGraphNode_AddParentFilter(FilterGraphNode* node, Filter* filter) {
    return node->AddParent(filter);
}

void DLL_EXPORT FilterGraphNode_DetachChild(FilterGraphNode* node, FilterGraphNode* child, bool mergeConnections) {
    node->DetachChild(child, mergeConnections);
}

void DLL_EXPORT FilterGraphNode_DetachChildren(FilterGraphNode* node) {
    node->DetachChildren();
}

void DLL_EXPORT FilterGraphNode_DetachParents(FilterGraphNode* node) {
    node->DetachParents();
}

void DLL_EXPORT FilterGraphNode_DetachFromGraph(FilterGraphNode* node, bool mergeConnections) {
    node->DetachFromGraph(mergeConnections);
}

void DLL_EXPORT FilterGraphNode_SwapChildren(FilterGraphNode* node, FilterGraphNode* with) {
    node->SwapChildren(with);
}
