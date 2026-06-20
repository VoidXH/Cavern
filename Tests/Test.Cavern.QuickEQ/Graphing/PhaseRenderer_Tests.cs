using Cavern;
using Cavern.QuickEQ.Graphing;

using Test.Cavern.QuickEQ.Consts;

namespace Test.Cavern.QuickEQ.Graphing;

/// <summary>
/// Tests the <see cref="PhaseRenderer"/> class.
/// </summary>
[TestClass]
public class PhaseRenderer_Tests {
    /// <summary>
    /// Tests if <see cref="PhaseRenderer.Normalize"/> works as intended.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Normalize() {
        PhaseRenderer renderer = new(128, 128);
        renderer.AddPhases(Listener.DefaultSampleRate, (TestSignals.sine440TF, Listener.DefaultSampleRate / 2, 0xFFFFFFFF));
        renderer.Normalize();
        Assert.IsTrue(renderer.Peak > 1, "The Peak should not be small.");
    }
}
