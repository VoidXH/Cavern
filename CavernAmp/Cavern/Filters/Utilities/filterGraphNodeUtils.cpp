#include <algorithm>
#include <cstring>

#include "filterGraphNodeUtils.h"

std::vector<FilterGraphNode*> FilterGraphNodeUtils::DeepCopy(
    const std::vector<FilterGraphNode*>& rootNodes) {
    auto result = DeepCopyWithMapping(rootNodes);
    return result.first;
}

std::pair<std::vector<FilterGraphNode*>, std::unordered_map<FilterGraphNode*, FilterGraphNode*>>
FilterGraphNodeUtils::DeepCopyWithMapping(
    const std::vector<FilterGraphNode*>& rootNodes) {
    std::unordered_map<FilterGraphNode*, FilterGraphNode*> mapping;

    std::function<FilterGraphNode*(FilterGraphNode*)> copyNode;
    copyNode = [&](FilterGraphNode* source) -> FilterGraphNode* {
        auto it = mapping.find(source);
        if (it != mapping.end()) {
            return it->second;
        }

        FilterGraphNode* copy = new FilterGraphNode(*source);
        mapping[source] = copy;
        for (size_t i = 0; i < source->GetChildren().size(); i++) {
            copy->AddChild(copyNode(source->GetChildren()[i]));
        }
        return copy;
    };

    std::vector<FilterGraphNode*> result;
    for (size_t i = 0; i < rootNodes.size(); i++) {
        result.push_back(copyNode(rootNodes[i]));
    }
    return std::make_pair(result, mapping);
}

bool FilterGraphNodeUtils::HasCycles(const std::vector<FilterGraphNode*>& rootNodes) {
    std::unordered_set<FilterGraphNode*> visited;
    std::unordered_set<FilterGraphNode*> inProgress;
    for (size_t i = 0; i < rootNodes.size(); i++) {
        if (visited.find(rootNodes[i]) == visited.end()) {
            if (HasCycles(rootNodes[i], visited, inProgress)) {
                return true;
            }
        }
    }
    return false;
}

bool FilterGraphNodeUtils::HasCycles(
    FilterGraphNode* currentNode,
    std::unordered_set<FilterGraphNode*>& visited,
    std::unordered_set<FilterGraphNode*>& inProgress) {
    if (inProgress.find(currentNode) != inProgress.end()) {
        return true;
    }
    if (visited.find(currentNode) != visited.end()) {
        return false;
    }

    inProgress.insert(currentNode);
    const auto& children = currentNode->GetChildren();
    for (size_t i = 0; i < children.size(); i++) {
        if (HasCycles(children[i], visited, inProgress)) {
            return true;
        }
    }
    inProgress.erase(currentNode);
    visited.insert(currentNode);
    return false;
}

std::unordered_set<FilterGraphNode*> FilterGraphNodeUtils::MapGraph(
    const std::vector<FilterGraphNode*>& rootNodes,
    std::function<const std::vector<FilterGraphNode*>& (FilterGraphNode*)> direction) {
    std::unordered_set<FilterGraphNode*> visited;
    std::queue<FilterGraphNode*> queue;
    for (size_t i = 0; i < rootNodes.size(); i++) {
        queue.push(rootNodes[i]);
    }

    while (!queue.empty()) {
        FilterGraphNode* currentNode = queue.front();
        queue.pop();

        if (visited.find(currentNode) != visited.end()) {
            continue;
        }

        visited.insert(currentNode);
        const auto& nextSteps = direction(currentNode);
        for (size_t i = 0; i < nextSteps.size(); i++) {
            queue.push(nextSteps[i]);
        }
    }

    return visited;
}

std::unordered_set<FilterGraphNode*> FilterGraphNodeUtils::MapGraph(
    const std::vector<FilterGraphNode*>& rootNodes) {
    return MapGraph(rootNodes, [](FilterGraphNode* node) -> const std::vector<FilterGraphNode*>& {
        return node->GetChildren();
    });
}

std::unordered_set<FilterGraphNode*> FilterGraphNodeUtils::MapGraphBack(
    const std::vector<FilterGraphNode*>& rootNodes) {
    return MapGraph(rootNodes, [](FilterGraphNode* node) -> const std::vector<FilterGraphNode*>& {
        return node->GetParents();
    });
}

bool FilterGraphNodeUtils::IsTopologicalSort(const std::vector<FilterGraphNode*>& orderedNodes) {
    std::unordered_set<FilterGraphNode*> visited;
    for (size_t i = 0; i < orderedNodes.size(); i++) {
        FilterGraphNode* node = orderedNodes[i];
        const auto& children = node->GetChildren();
        for (size_t j = 0; j < children.size(); j++) {
            if (visited.find(children[j]) != visited.end()) {
                return false;
            }
        }
        visited.insert(node);
    }
    return true;
}

std::vector<FilterGraphNode*> FilterGraphNodeUtils::TopologicalSort(
    const std::vector<FilterGraphNode*>& rootNodes) {
    std::vector<FilterGraphNode*> result;
    std::unordered_set<FilterGraphNode*> visited;

    std::function<void(FilterGraphNode*)> visitNode;
    visitNode = [&](FilterGraphNode* node) {
        if (visited.find(node) != visited.end()) {
            return;
        }
        visited.insert(node);
        const auto& children = node->GetChildren();
        for (size_t i = 0; i < children.size(); i++) {
            visitNode(children[i]);
        }
        result.insert(result.begin(), node);
    };

    for (size_t i = 0; i < rootNodes.size(); i++) {
        visitNode(rootNodes[i]);
    }
    return result;
}

void FilterGraphNodeUtils::ConvertToConvolution(
    FilterGraphNode* node, int sampleRate, int filterLength) {
    float* impulse = new float[filterLength]();
    impulse[0] = 1;

    FilterGraphNode* downmergeUntil = node;
    while (true) {
        downmergeUntil->pFilter->Process(impulse, filterLength);
        const auto& children = downmergeUntil->GetChildren();
        if (children.size() != 1 || downmergeUntil->GetParents().size() != 1) {
            break;
        }
        downmergeUntil = children[0];
    }

    const auto& newChildrenList = downmergeUntil->GetChildren();
    std::vector<FilterGraphNode*> newChildren(newChildrenList.begin(), newChildrenList.end());
    downmergeUntil->DetachChildren();

    node->pFilter = new FastConvolver(impulse, filterLength, 0);
    delete[] impulse;

    node->DetachChildren();
    for (size_t i = 0; i < newChildren.size(); i++) {
        node->AddChild(newChildren[i]);
    }
}

void FilterGraphNodeUtils::ConvertToConvolution(
    const std::vector<FilterGraphNode*>& rootNodes, int sampleRate, int filterLength) {
    std::unordered_set<FilterGraphNode*> visited;
    std::queue<FilterGraphNode*> queue;
    for (size_t i = 0; i < rootNodes.size(); i++) {
        queue.push(rootNodes[i]);
    }

    while (!queue.empty()) {
        FilterGraphNode* currentNode = queue.front();
        queue.pop();

        if (visited.find(currentNode) != visited.end()) {
            continue;
        }

        ConvertToConvolution(currentNode, sampleRate, filterLength);
        visited.insert(currentNode);

        const auto& children = currentNode->GetChildren();
        for (size_t i = 0; i < children.size(); i++) {
            queue.push(children[i]);
        }
    }
}
