using Cavern.Filters;
using Cavern.Format.FilterSet;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Format.FilterSet;

/// <summary>
/// Tests the <see cref="EmotivaFilterSet"/> class.
/// </summary>
[TestClass]
public class Emotiva_Tests {
    /// <summary>
    /// Tests if the snapped Q factor never reaches 0, as a Q of 0 produces a degenerate biquad
    /// (division by zero in the coefficient calculation).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void QFactorNeverZero() {
        // Emotiva rounds Q to the nearest 0.2 (Math.Round(Q * 5) / 5).
        // A Q of 0.04 is an Emotiva number that rounds down to 0.
        const double q = .04;
        PeakingEQ filter = new PeakingEQ(Constants.sampleRate, 1000, q, 0);
        EmotivaFilterSet set = new EmotivaFilterSet(1, Constants.sampleRate);
        set.SetupChannel(0, [filter]);

        double snappedQ = set.SnapQ(filter.Q);
        Assert.IsTrue(snappedQ > 0, "The snapped Q factor must never reach 0.");
    }
}
