using VoidX.WPF.FFmpeg;

namespace Cavernize.Logic.Rendering;

/// <summary>
/// FFmpeg locator/runner that reports status through a callback.
/// </summary>
public sealed class StatusFFmpeg : FFmpeg {
    readonly Action<string> statusChanged;

    /// <summary>
    /// FFmpeg locator/runner that reports status through a callback.
    /// </summary>
    public StatusFFmpeg(Action<string> statusChanged, string lastLocation = null) {
        this.statusChanged = statusChanged;
        Location = lastLocation;
    }

    /// <inheritdoc/>
    public override void UpdateStatusText(string text) => statusChanged?.Invoke(text);

    /// <summary>
    /// Create and initialize an FFmpeg runner.
    /// </summary>
    public static StatusFFmpeg Create(Action<string> statusChanged, string lastLocation = null) {
        StatusFFmpeg result = new(statusChanged, lastLocation);
        result.CheckFFmpeg();
        return result;
    }
}
