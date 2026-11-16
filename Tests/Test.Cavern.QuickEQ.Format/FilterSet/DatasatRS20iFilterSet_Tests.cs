using Cavern.Format.FilterSet;

using Test.Cavern.QuickEQ.Format.FilterSet.TestEnvironment;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests if <see cref="DatasatRS20iFilterSet"/>s are handled properly.
/// </summary>
[TestClass]
public class DatasatRS20iFilterSet_Tests : IIRFilterSetJig {
    /// <summary>
    /// Tests if <see cref="DatasatRS20iFilterSet"/>s are handled properly.
    /// </summary>
    public DatasatRS20iFilterSet_Tests() : base(FilterSetTarget.DatasatRS20i) => Tolerance = 3.4;
}
