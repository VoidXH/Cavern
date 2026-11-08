using Cavern.Format.Measurements;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests the <see cref="ADYMeasurement"/> class.
/// </summary>
[TestClass]
public class ADYMeasurement_Tests {
    /// <summary>
    /// Tests if a valid measurement file can be parsed with <see cref="ADYMeasurement.FromFile(string)"/>.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void Import() {
        ADYMeasurement measurement = new();
        measurement.FromFile(Path.Combine(Constants.testData, "Configurations", "MultEQ XT32 Marantz AV20.ady"));
    }
}
