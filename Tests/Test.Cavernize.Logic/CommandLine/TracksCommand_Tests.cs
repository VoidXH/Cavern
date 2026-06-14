using System.Reflection;

using Cavernize.Logic.CommandLine;
using Cavernize.Logic.Language;
using VoidX.WPF.Language;

using Test.Cavernize.Logic.Utilities;

namespace Test.Cavernize.Logic.CommandLine;

/// <summary>
/// Tests the -tracks command line parameter.
/// </summary>
[TestClass]
public class TracksCommand_Tests {
    /// <summary>
    /// Tests if a non-container standalone audio file also works with the -tracks command
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SupportedForAudioFile() {
        string previousCultureOverride = LanguageSettings.CultureOverride;
        try {
            LanguageSettings.CultureOverride = "en-US";
            ResetTrackStrings();
            MockCavernizeApp app = new MockCavernizeApp();
            (string output, string error) = ConsoleUtils.Redirect(() => CommandLineProcessor.Initialize([
                "-i", Constants.emptyWavTestFile, "-trks"
            ], app));

            Assert.AreEqual(output, "[0] PCM (integer)" + Environment.NewLine, "A single PCM track must be found.");
            Assert.AreEqual(string.Empty, error, "An error has happened: " + error);
        } finally {
            LanguageSettings.CultureOverride = previousCultureOverride;
            ResetTrackStrings();
        }
    }

    static void ResetTrackStrings() => typeof(LanguageBase<TrackStrings>)
        .GetField("active", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
}
