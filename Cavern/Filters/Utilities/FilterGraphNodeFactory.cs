using System;

using Cavern.Filters.Interfaces;
using Cavern.Utilities;

namespace Cavern.Filters.Utilities {
    /// <summary>
    /// Creates <see cref="IFilterGraphNode"/> instances depending on if the filter was initialized for CavernAmp.
    /// When mixing .NET and CavernAmp filters, use the <see cref="FilterGraphNode"/> class directly, which can handle both types of filters.
    /// </summary>
    public static class FilterGraphNodeFactory {
        /// <summary>
        /// Creates <see cref="IFilterGraphNode"/> instances depending on if the filter was initialized for CavernAmp.
        /// When mixing .NET and CavernAmp filters, use the <see cref="FilterGraphNode"/> class directly, which can handle both types of filters.
        /// </summary>
        public static IFilterGraphNode Create(IFilter filter) => CavernAmp.Available && filter is FilterAmp filterAmp ?
            (IFilterGraphNode)new FilterGraphNodeAmp(filterAmp) :
            filter is Filter filterManaged ?
                new FilterGraphNode(filterManaged) :
                throw new InvalidOperationException("Unsupported filter type for graph node creation.");
    }
}
