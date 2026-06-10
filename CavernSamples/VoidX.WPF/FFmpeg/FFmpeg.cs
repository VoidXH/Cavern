using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace VoidX.WPF.FFmpeg;

/// <summary>
/// FFmpeg runner and locator.
/// </summary>
public abstract class FFmpeg {
    /// <summary>
    /// In console mode, output less and launch in the same console.
    /// </summary>
    public static bool ConsoleMode { get; set; }

    /// <summary>
    /// Displayed status message when FFmpeg was found.
    /// </summary>
    public static string ReadyText { get; set; } = "Ready.";

    /// <summary>
    /// Displayed status message when FFmpeg was not found.
    /// </summary>
    public static string NotReadyText { get; set; } = "FFmpeg isn't found, codec limitations are applied.";

    /// <summary>
    /// FFmpeg is located and ready to use.
    /// </summary>
    public bool Found { get; private set; }

    /// <summary>
    /// Last user-selected location of FFmpeg.
    /// </summary>
    public string Location {
        get => location;
        set {
            location = value;
            CheckFFmpeg();
        }
    }
    string location;

    /// <summary>
    /// If the program exists in PATH, its full path is returned.
    /// </summary>
    static string GetPathOfProgram(string key) {
        try {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = isWindows ? "where" : "which",
                    Arguments = key,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!isWindows) {
                string path = Environment.GetEnvironmentVariable("PATH");
                process.StartInfo.Environment["PATH"] = (string.IsNullOrEmpty(path) ? string.Empty :
                    path + Path.PathSeparator) + "/opt/homebrew/bin:/usr/local/bin:/opt/local/bin:/usr/bin:/bin";
            }

            process.Start();
            string output = process.StandardOutput.ReadLine();
            process.WaitForExit();
            if (!string.IsNullOrWhiteSpace(output)) {
                return output;
            }
        } catch {
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            foreach (string directory in new[] { "/opt/homebrew/bin", "/usr/local/bin", "/opt/local/bin",
                "/usr/bin", "/bin" }) {
                string path = Path.Combine(directory, key);
                if (File.Exists(path)) {
                    return path;
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Display the status of FFmpeg.
    /// </summary>
    public abstract void UpdateStatusText(string text);

    /// <summary>
    /// Checks if FFmpeg's executable is located at the selected directory and update the UI accordingly.
    /// </summary>
    public void CheckFFmpeg() {
        if (string.IsNullOrEmpty(location) || !File.Exists(location)) {
            string detected = GetPathOfProgram("ffmpeg");
            if (!string.IsNullOrEmpty(detected)) {
                location = detected;
            }
        }
        Found = !string.IsNullOrEmpty(location) && File.Exists(location);
        UpdateStatusText(Found ? ReadyText : NotReadyText);
    }

    /// <summary>
    /// Launch the FFmpeg to process a file with the given arguments.
    /// </summary>
    public bool Launch(string arguments) {
        Console.WriteLine("Launching FFmpeg with the following arguments: " + arguments);
        ProcessStartInfo start = new() {
            Arguments = ConsoleMode ? arguments + lesserOutput : arguments,
            FileName = Location,
            UseShellExecute = false
        };
        try {
            using Process proc = Process.Start(start);
            proc.WaitForExit();
            return proc.ExitCode == 0;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Arguments that limit the text FFmpeg writes to the console not to flood it in console mode.
    /// </summary>
    const string lesserOutput = " -v error -stats";
}
