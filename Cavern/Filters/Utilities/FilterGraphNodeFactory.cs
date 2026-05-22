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
        public static IFilterGraphNode Create(Filter filter) => CavernAmp.Available && filter is FilterAmp filterAmp ?
            (IFilterGraphNode)new FilterGraphNodeAmp(filterAmp) :
            new FilterGraphNode(filter);
    }
}
