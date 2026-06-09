using System.Diagnostics;

namespace Test.CavernizeGUI.Utilities;

/// <summary>
/// Handlers for the Cavernize application.
/// </summary>
static class CavernizeUtils {
    /// <summary>
    /// Get where the executable for Cavernize is located.
    /// </summary>
    public static string GetCavernizeLocation() {
        string result = Path.Combine("..", "..", "..", "..", "CavernSamples", "CavernizeGUI", "bin", "Release", "net8.0-windows", "CavernizeGUI.exe");
        Assert.IsTrue(File.Exists(result), "CavernizeGUI.exe not found at: " + Path.GetFullPath(result));
        return result;
    }

    /// <summary>
    /// Start a Cavernize instance with the given <paramref name="arguments"/> and wait a specific <paramref name="timeout"/> (seconds)
    /// before forcefully shutting it down. The standard output and error of the returned <see cref="Process"/> will be readable.
    /// </summary>
    public static Process LaunchCavernize(string arguments, int timeout) {
        Process process = Process.Start(new ProcessStartInfo {
            FileName = GetCavernizeLocation(),
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        });

        Assert.IsNotNull(process, "Failed to start Cavernize.");
        if (!process.WaitForExit(timeout * 1000)) {
            process.Kill();
            Assert.Fail("Cavernize didn't exit.");
        }

        if (process.ExitCode != 0) {
            string error = process.StandardError.ReadToEnd();
            Assert.Fail($"Cavernize crashed with exit code {process.ExitCode}. Error output:\n{error}");
        }
        return process;
    }
}
