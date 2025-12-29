using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Test.Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Steps that are commonly needed in tests using <see cref="FilterGraphNode"/>s.
    /// </summary>
    static class FilterGraphNodeTestSteps {
        /// <summary>
        /// Checks if a channel's input <paramref name="node"/> reaches the same channel's output.
        /// </summary>
        public static void AssertReachesOwnOutput(this FilterGraphNode node) {
            if (node.Filter is not InputChannel input) {
                Assert.Fail($"An input channel node ({node}) is not an InputChannel.");
                return;
            }

            ReferenceChannel channel = input.Channel;
            HashSet<FilterGraphNode> map = FilterGraphNodeUtils.MapGraph([node]);
            if (node.Children.Count == 0 || !map.Any(x => x.Filter is OutputChannel output && output.Channel == channel && x.Children.Count == 0)) {
                Assert.Fail($"{channel} is not connected to its own output after crossover.");
            }
        }

        /// <summary>
        /// Checks if a channel's output <paramref name="node"/> reaches the same channel's input.
        /// </summary>
        public static void AssertReachesOwnInput(this FilterGraphNode node) {
            if (node.Filter is not OutputChannel output) {
                Assert.Fail($"An output channel node ({node}) is not an OutputChannel.");
                return;
            }

            ReferenceChannel channel = output.Channel;
            HashSet<FilterGraphNode> map = FilterGraphNodeUtils.MapGraphBack([node]);
            if (node.Parents.Count == 0 || !map.Any(x => x.Filter is InputChannel input && input.Channel == channel && x.Parents.Count == 0)) {
                Assert.Fail($"{channel} is not connected to its own input after crossover.");
            }
        }
    }
}
