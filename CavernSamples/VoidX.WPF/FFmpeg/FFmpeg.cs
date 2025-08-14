using System.Diagnostics;
using System.IO;

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
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "where",
                    Arguments = key,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadLine();
            process.WaitForExit();
            return string.IsNullOrWhiteSpace(output) ? string.Empty : output;
        } catch {
            return string.Empty;
        }
    }

    /// <summary>
    /// Display the status of FFmpeg.
    /// </summary>
    public abstract void UpdateStatusText(string text);

    /// <summary>
    /// Checks if FFmpeg's executable is located at the selected directory and update the UI accordingly.
    /// </summary>
    public void CheckFFmpeg() {
        if (string.IsNullOrEmpty(location)) {
            location = GetPathOfProgram("ffmpeg");
        }
        Found = !string.IsNullOrEmpty(location) && File.Exists(location);
        UpdateStatusText(Found ? ReadyText : NotReadyText);
    }

    /// <summary>
    /// Launch the FFmpeg to process a file with the given arguments.
    /// </summary>
    public bool Launch(string arguments) {
        ProcessStartInfo start = new() {
            Arguments = ConsoleMode ? arguments + lesserOutput : arguments,
            FileName = Location,
            UseShellExecute = !ConsoleMode
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
