namespace Test.Cavern.QuickEQ.FilterSet.Exceptions {
    /// <summary>
    /// Tells if a filter gain is out of the device range.
    /// </summary>
    public class GainOutOfRangeException(double gain, double minGain, double maxGain) : Exception(string.Format(message, gain, minGain, maxGain)) {
        const string message = "Filter gain ({0} dB) is out of the device range ({1}-{2} dB)";
    }
}
