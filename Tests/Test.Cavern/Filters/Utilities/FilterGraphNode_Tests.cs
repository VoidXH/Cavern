using Test.Cavern.Consts;

using Cavern.Filters.Utilities;
using Cavern.Filters;

namespace Test.Cavern.Filters.Utilities;

/// <summary>
/// Used for the <see cref="FilterGraphNodeFactory"/> to create the proper type of <see cref="IFilterGraphNode"/> depending on CavernAmp availability.
/// </summary>
/// <param name="handle"></param>
class MockFilterAmp() : FilterAmp(default) {
    /// <inheritdoc/>
    public override object Clone() => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void Dispose() => throw new NotImplementedException();
}

/// <summary>
/// Tests the <see cref="FilterGraphNode"/> class.
/// </summary>
[TestClass]
public class FilterGraphNode_Tests {
    /// <summary>
    /// Tests if an <see cref="IFilterGraphNode"/> is created and disposed properly.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Lifecycle() => CavernAmpTest.Run(() => {
        using IFilterGraphNode node = FilterGraphNodeFactory.Create(new MockFilterAmp());
    });
}
