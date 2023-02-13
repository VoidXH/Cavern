using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace VoidX.WPF {
    /// <summary>
    /// FFmpeg runner and locator.
    /// </summary>
    public class FFmpeg {
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
        public string Location { get; private set; }

        /// <summary>
        /// Status text display.
        /// </summary>
        readonly TextBlock statusText;

        /// <summary>
        /// FFmpeg runner and locator.
        /// </summary>
        public FFmpeg(TextBlock statusText, string lastLocation) {
            this.statusText = statusText;
            Location = lastLocation;
            CheckFFmpeg();
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
        /// Prompts the user to select FFmpeg's location.
        /// </summary>
        public void Locate() {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = filter
            };
            if (dialog.ShowDialog().Value) {
                Location = dialog.FileName;
                CheckFFmpeg();
            }
        }

        /// <summary>
        /// Checks if FFmpeg's executable is located at the selected directory and update the UI accordingly.
        /// </summary>
        public void CheckFFmpeg() {
            Found = !string.IsNullOrEmpty(Location) && File.Exists(Location);
            if (statusText != null) {
                if (Found) {
                    statusText.Text = ReadyText;
                } else {
                    statusText.Text = NotReadyText;
                }
            }
        }

        /// <summary>
        /// Open file dialog filter for selecting FFmpeg's binary.
        /// </summary>
        const string filter = "FFmpeg|ffmpeg.exe";

        /// <summary>
        /// Arguments that limit the text FFmpeg writes to the console not to flood it in console mode.
        /// </summary>
        const string lesserOutput = " -v error -stats";
    }
}