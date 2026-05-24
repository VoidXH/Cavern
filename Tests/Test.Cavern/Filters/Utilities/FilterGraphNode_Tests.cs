using Cavern.Filters.Utilities;
using Cavern.Filters;
using Cavern.Filters.Interfaces;

using Test.Cavern.Consts;

namespace Test.Cavern.Filters.Utilities;

/// <summary>
/// Tests the <see cref="FilterGraphNode"/> class.
/// </summary>
[TestClass]
public class FilterGraphNode_Tests {
    /// <summary>
    /// Create a random filter for testing equivalencies.
    /// </summary>
    static IGainFilter RandomFilter => FilterFactory.CreateGain(0);

    /// <summary>
    /// Tests if an <see cref="IFilterGraphNode"/> is created and disposed properly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Lifecycle() => CavernAmpTest.Run(() => {
        using IFilterGraphNode node = FilterGraphNodeFactory.Create(RandomFilter);
    });

    /// <summary>
    /// Tests that a diamond-shaped filter graph topology correctly traces back to a single root.
    /// Verifies that traversing up from commonChild's parents finds the same root.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void DiamondTopology() => CavernAmpTest.Run(() => {
        static IFilterGraphNode GetRootParent(IFilterGraphNode node) {
            IFilterGraphNode current = node;
            while (current.Parents.Count > 0) {
                current = current.Parents[0];
            }
            return current;
        }

        using IFilterGraphNode root = FilterGraphNodeFactory.Create(RandomFilter);
        using IFilterGraphNode child1 = FilterGraphNodeFactory.Create(RandomFilter);
        using IFilterGraphNode child2 = FilterGraphNodeFactory.Create(RandomFilter);
        using IFilterGraphNode commonChild = FilterGraphNodeFactory.Create(RandomFilter);
        Assert.AreNotEqual(child1, child2);

        root.AddChild(child1);
        root.AddChild(child2);
        child1.AddChild(commonChild);
        child2.AddChild(commonChild);

        // commonChild should have two parents
        Assert.AreEqual(2, commonChild.Parents.Count);
        Assert.AreEqual(child1, commonChild.Parents[0]);
        Assert.AreEqual(child2, commonChild.Parents[1]);

        // Tracing up through either parent should reach the same root
        IFilterGraphNode rootFromChild1 = GetRootParent(child1);
        IFilterGraphNode rootFromChild2 = GetRootParent(child2);

        Assert.AreEqual(root, rootFromChild1);
        Assert.AreEqual(root, rootFromChild2);
    });
}
