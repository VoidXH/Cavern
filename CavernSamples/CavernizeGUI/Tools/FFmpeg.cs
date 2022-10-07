using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;

using CavernizeGUI;

using Button = System.Windows.Controls.Button;

namespace VoidX.WPF {
    /// <summary>
    /// FFmpeg runner and locator.
    /// </summary>
    public class FFmpeg {
        /// <summary>
        /// Last user-selected location of FFmpeg.
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// The button that starts the process that requires FFmpeg.
        /// </summary>
        readonly Button start;

        /// <summary>
        /// Status text display.
        /// </summary>
        readonly TextBlock statusText;

        /// <summary>
        /// FFmpeg runner and locator.
        /// </summary>
        public FFmpeg(Button start, TextBlock statusText, string lastLocation) {
            this.start = start;
            this.statusText = statusText;
            Location = lastLocation;
            CheckFFmpeg();
        }

        /// <summary>
        /// Launch the FFmpeg to process a file with the given arguments.
        /// </summary>
        public bool Launch(string arguments) {
            ProcessStartInfo start = new() {
                Arguments = Program.ConsoleMode ? arguments + lesserOutput : arguments,
                FileName = Path.Combine(Location, exeName),
                UseShellExecute = !Program.ConsoleMode
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
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK) {
                Location = dialog.SelectedPath;
                CheckFFmpeg();
            }
        }

        /// <summary>
        /// Checks if FFmpeg's executable is located at the selected directory and update the UI accordingly.
        /// </summary>
        public bool CheckFFmpeg() {
            bool found = start.IsEnabled = !string.IsNullOrEmpty(Location) && File.Exists(Path.Combine(Location, exeName));
            if (found)
                statusText.Text = readyText;
            else
                statusText.Text = notReadyText;
            return found;
        }

        /// <summary>
        /// Filename of the searched executable.
        /// </summary>
        const string exeName = "ffmpeg.exe";

        /// <summary>
        /// Displayed status message when FFmpeg was found.
        /// </summary>
        const string readyText = "Ready.";

        /// <summary>
        /// Displayed status message when FFmpeg was not found.
        /// </summary>
        const string notReadyText = "FFmpeg isn't found, please locate.";

        /// <summary>
        /// Arguments that limit the text FFmpeg writes to the console not to flood it in console mode.
        /// </summary>
        const string lesserOutput = " -v error -stats";
    }
}