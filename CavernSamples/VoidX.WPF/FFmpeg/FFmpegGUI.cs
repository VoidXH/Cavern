using Microsoft.Win32;
using System.Windows.Controls;

namespace VoidX.WPF.FFmpeg;

/// <summary>
/// FFmpeg runner and locator with <see cref="TaskEngine"/> support.
/// </summary>
public sealed class FFmpegGUI : FFmpeg {
    /// <summary>
    /// Status text display.
    /// </summary>
    readonly TextBlock statusText;

    /// <summary>
    /// FFmpeg runner and locator with <see cref="TaskEngine"/> support.
    /// </summary>
    public FFmpegGUI(TextBlock statusText, string lastLocation) {
        this.statusText = statusText;
        Location = lastLocation;
    }

    /// <inheritdoc/>
    public override void UpdateStatusText(string text) {
        if (statusText != null) {
            statusText.Text = text;
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
    /// Open file dialog filter for selecting FFmpeg's binary.
    /// </summary>
    const string filter = "FFmpeg|ffmpeg.exe";
}
