using System.Diagnostics;

using Test.CavernizeGUI.Utilities;

namespace Test.CavernizeGUI;

/// <summary>
/// Tests the -help command line argument.
/// </summary>
[TestClass]
public class HelpArgument_Tests {
    /// <summary>
    /// Tests if running CavernizeGUI with -h only returns the help, not an extra (stack) trace for example.
    /// </summary>
    [TestMethod, Timeout(30000)]
    public void HelpArgument_Short_OnlyHelpShown() => TestHelp("-h");

    /// <summary>
    /// Tests if running CavernizeGUI with -help only returns the help, not an extra (stack) trace for example.
    /// </summary>
    [TestMethod, Timeout(30000)]
    public void HelpArgument_Long_OnlyHelpShown() => TestHelp("-help");

    /// <summary>
    /// Tests if running CavernizeGUI with a given help <paramref name="argument"/> only returns the help, not an extra (stack) trace for example.
    /// </summary>
    static void TestHelp(string argument) {
        using Process process = CavernizeUtils.LaunchCavernize(argument, 30);
        string[] output = process.StandardOutput.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in output) {
            if (!line.StartsWith('-') || !line.Contains(':')) {
                Assert.Fail("Non-help item shown: " + line);
            }
        }
    }
}
