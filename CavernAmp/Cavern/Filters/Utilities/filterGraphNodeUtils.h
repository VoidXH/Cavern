#ifndef FILTERGRAPHNODEUTILS_H
#define FILTERGRAPHNODEUTILS_H

#include <functional>
#include <queue>
#include <unordered_set>
#include <unordered_map>
#include <vector>

#include "../../../export.h"
#include "../fastConvolver.h"
#include "filterGraphNode.h"

/// \brief Special functions for handling FilterGraphNodes.
class DLL_EXPORT FilterGraphNodeUtils {
public:
    /// Convert the filter graph's filters to convolutions, and merge chains together to a single filter.
    /// \param rootNodes All root nodes (nodes with no parents)
    /// \param sampleRate Audio sample rate
    /// \param filterLength Length of the convolution filter
    static void ConvertToConvolution(const std::vector<FilterGraphNode*>& rootNodes, int sampleRate, int filterLength);

    /// Creates a copy of the complete graph with no overlapping memory with the old rootNodes.
    /// \param rootNodes All root nodes
    /// \returns Vector of cloned root nodes
    static std::vector<FilterGraphNode*> DeepCopy(const std::vector<FilterGraphNode*>& rootNodes);

    /// Creates a copy of the complete graph with no overlapping memory with the old rootNodes,
    /// and also results which old root maps to which new one.
    /// \param rootNodes All root nodes
    /// \returns Pair of cloned root nodes and mapping from old to new
    static std::pair<std::vector<FilterGraphNode*>, std::unordered_map<FilterGraphNode*, FilterGraphNode*>>
        DeepCopyWithMapping(const std::vector<FilterGraphNode*>& rootNodes);

    /// Check if the graph has cycles.
    /// \param rootNodes All nodes which have no parents
    /// \returns true if the graph has cycles
    static bool HasCycles(const std::vector<FilterGraphNode*>& rootNodes);

    /// Checks if a list of nodes is topologically sorted.
    /// \param orderedNodes Ordered list of nodes
    /// \returns true if every child appears after its parents
    static bool IsTopologicalSort(const std::vector<FilterGraphNode*>& orderedNodes);

    /// Get all nodes in a filter graph knowing the root nodes.
    /// \param rootNodes All nodes which have no parents
    /// \param direction Traversal direction (default: children)
    /// \returns Set of all reachable nodes
    static std::unordered_set<FilterGraphNode*> MapGraph(
        const std::vector<FilterGraphNode*>& rootNodes,
        std::function<const std::vector<FilterGraphNode*>& (FilterGraphNode*)> direction);

    /// Get all nodes in a filter graph knowing the root nodes (default direction: children).
    static std::unordered_set<FilterGraphNode*> MapGraph(const std::vector<FilterGraphNode*>& rootNodes);

    /// Get all nodes in a filter graph knowing the end nodes by discovering parents.
    /// \param rootNodes All output nodes
    /// \returns Set of all reachable nodes going backwards
    static std::unordered_set<FilterGraphNode*> MapGraphBack(const std::vector<FilterGraphNode*>& rootNodes);

    /// Return all nodes on the graph in an order where every child is after its parents.
    /// \param rootNodes All root nodes (nodes with no parents)
    /// \returns Nodes in topological order (children after parents)
    static std::vector<FilterGraphNode*> TopologicalSort(const std::vector<FilterGraphNode*>& rootNodes);

private:
    /// Converts this filter to a convolution and upmerges all children until possible.
    static void ConvertToConvolution(FilterGraphNode* node, int sampleRate, int filterLength);

    /// Starting from a single node, checks if the graph has cycles.
    static bool HasCycles(FilterGraphNode* currentNode,
                          std::unordered_set<FilterGraphNode*>& visited,
                          std::unordered_set<FilterGraphNode*>& inProgress);

 };

#endif // FILTERGRAPHNODEUTILS_H
