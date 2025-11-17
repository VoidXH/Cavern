namespace Test.Cavern.QuickEQ.FilterSet.Exceptions {
    /// <summary>
    /// Tells if a filter gain is not within tolerance to the required precision.
    /// </summary>
    public class GainImpreciseException(double gain, double expectedPrecision) : Exception(string.Format(message, gain, expectedPrecision)) {
        const string message = "Filter gain ({0} dB) is not aligned to the allowed precision ({1} dB)";
    }
}
