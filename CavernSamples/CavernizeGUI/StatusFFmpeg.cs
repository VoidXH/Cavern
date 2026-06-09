using VoidX.WPF.FFmpeg;

namespace CavernizeGUI;

/// <summary>
/// FFmpeg locator/runner that reports status through a callback.
/// </summary>
public sealed class StatusFFmpeg : FFmpeg {
    readonly Action<string> statusChanged;

    /// <summary>
    /// FFmpeg locator/runner that reports status through a callback.
    /// </summary>
    public StatusFFmpeg(Action<string> statusChanged) : this(statusChanged, null) { }

    /// <summary>
    /// FFmpeg locator/runner that reports status through a callback.
    /// </summary>
    public StatusFFmpeg(Action<string> statusChanged, string lastLocation) {
        this.statusChanged = statusChanged;
        Location = lastLocation;
    }

    /// <inheritdoc/>
    public override void UpdateStatusText(string text) => statusChanged?.Invoke(text);

    /// <summary>
    /// Create and initialize an FFmpeg runner.
    /// </summary>
    public static StatusFFmpeg Create(Action<string> statusChanged) => Create(statusChanged, null);

    /// <summary>
    /// Create and initialize an FFmpeg runner.
    /// </summary>
    public static StatusFFmpeg Create(Action<string> statusChanged, string lastLocation) {
        StatusFFmpeg result = new(statusChanged, lastLocation);
        result.CheckFFmpeg();
        return result;
    }
}
