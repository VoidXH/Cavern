using Cavern.Filters;

namespace Test.Cavern.QuickEQ.Utilities {
    /// <summary>
    /// A filter that has to be found for tests that check for the presence of a filter.
    /// </summary>
    class WaldoFilter : Filter {
        /// <inheritdoc/>
        public override object Clone() => new WaldoFilter();
    }
}
