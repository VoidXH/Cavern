using Cavern.Format.FilterSet.Measurements;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests the <see cref="MQXMeasurement"/> class.
/// </summary>
[TestClass]
public class MQXMeasurement_Tests {
    /// <summary>
    /// Tests if a valid measurement file can be parsed with <see cref="MQXMeasurement.FromFile(string)"/>.
    /// </summary>
    [TestMethod, Timeout(3000)]
    public void Import() {
        MQXMeasurement measurement = new();
        measurement.FromFile(Path.Combine(Constants.testData, "Configurations", "MultEQ-X Marantz AV7706.mqx"));
    }
}
