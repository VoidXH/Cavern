namespace Test.Cavern.QuickEQ.FilterSet.Exceptions {
    /// <summary>
    /// Tells if a filter's Q factor is not within tolerance to the required precision.
    /// </summary>
    public class QImpreciseException(double q, double expectedPrecision) : Exception(string.Format(message, q, expectedPrecision)) {
        const string message = "The filter's Q factor ({0}) is not aligned to the allowed precision ({1})";
    }
}
