using Cavern.Format.FilterSet;

using Test.Cavern.QuickEQ.FilterSet.TestEnvironment;

namespace Test.Cavern.QuickEQ.FilterSet {
    /// <summary>
    /// Tests if <see cref="StormAudioFilterSet"/>s are handled properly.
    /// </summary>
    [TestClass]
    public class Rotel_Tests : IIRFilterSetJig {
        /// <summary>
        /// Tests if <see cref="StormAudioFilterSet"/>s are handled properly.
        /// </summary>
        public Rotel_Tests() : base(FilterSetTarget.Rotel) { }
    }
}
