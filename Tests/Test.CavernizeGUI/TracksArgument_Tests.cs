using System.Diagnostics;

using Test.CavernizeGUI.Utilities;

namespace Test.CavernizeGUI;

/// <summary>
/// Tests the -tracks command line argument.
/// </summary>
[TestClass]
public class TracksArgument_Tests {
    /// <summary>
    /// Tests if running CavernizeGUI with -tracks only returns what's actually in the test file.
    /// </summary>
    [TestMethod, Timeout(10000)]
    public void HelpArgument_Short_OnlyHelpShown() {
        string file = Path.GetFullPath(Constants.multitrackTestFile);
        using Process process = CavernizeUtils.LaunchCavernize($"-i \"{file}\" -tracks", 10);
        string[] output = process.StandardOutput.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.AreEqual(4, output.Length);
        Assert.AreEqual("[X] HEVC (not audio)", output[0]);
        Assert.AreEqual("[0] Unknown (und)", output[1]);
        Assert.AreEqual("[1] Enhanced AC-3 (und)", output[2]);
        Assert.AreEqual("[X] EnhancedAC3 (not audio)", output[3]);
    }
}
