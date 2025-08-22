namespace Cavernize.Logic.Rendering;

/// <summary>
/// Tells that an operation is already running.
/// </summary>
public class ConcurrencyException(string message) : Exception(message) {
}

/// <summary>
/// Tells that some applied settings are not compatible with each other.
/// </summary>
public class IncompatibleSettingsException(string message) : Exception(message) {
}

/// <summary>
/// Tells if the requested channel count exceeds the maximum allowed by the format.
/// </summary>
public class OverMaxChannelsException(int channels, int maxChannels) : Exception(string.Format(message, channels, maxChannels)) {
    /// <summary>
    /// Number of requested channels.
    /// </summary>
    public int Channels { get; private set; } = channels;

    /// <summary>
    /// Number of maximum allowed channels.
    /// </summary>
    public int MaxChannels { get; private set; } = maxChannels;

    const string message = "The requested channel count of {0} exceeds the maximum allowed of {1} for this format.";
}

/// <summary>
/// Tells that a track setup is wrong or unsupported.
/// </summary>
public class TrackException(string message) : Exception(message) {
}
