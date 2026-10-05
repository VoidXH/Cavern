using Cavern.Format.FilterSet;

using Test.Cavern.QuickEQ.Format.FilterSet.TestEnvironment;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests if <see cref="StormAudioFilterSet"/>s are handled properly.
/// </summary>
[TestClass]
public class StormAudioFilterSet_Tests : IIRFilterSetJig {
    /// <summary>
    /// Tests if <see cref="StormAudioFilterSet"/>s are handled properly.
    /// </summary>
    public StormAudioFilterSet_Tests() : base(FilterSetTarget.StormAudio) { }
}
