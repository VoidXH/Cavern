namespace Test.CavernizeGUI;

/// <summary>
/// Constant test values.
/// </summary>
static class Constants {
    /// <summary>
    /// Path of files needed for testing.
    /// </summary>
    internal const string cavernFormatTestData = "../../../Test.Cavern.Format/TestData";

    /// <summary>
    /// A file containing a video, an unknown audio, an audio with signal, and a silent audio.
    /// </summary>
    internal static readonly string multitrackTestFile = Path.Combine(cavernFormatTestData, "Multitrack.mkv");
}
