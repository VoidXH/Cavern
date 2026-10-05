using Cavern.Format.FilterSet;

using Test.Cavern.QuickEQ.Format.FilterSet.TestEnvironment;

namespace Test.Cavern.QuickEQ.Format.FilterSet;
/// <summary>
/// Tests if <see cref="RotelFilterSet"/>s are handled properly.
/// </summary>
[TestClass]
public class RotelFilterSet_Tests : IIRFilterSetJig {
    /// <summary>
    /// Tests if <see cref="RotelFilterSet"/>s are handled properly.
    /// </summary>
    public RotelFilterSet_Tests() : base(FilterSetTarget.Rotel) => Tolerance = 3.4;
}
