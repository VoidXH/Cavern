namespace VoidX.WPF.FFmpeg;

/// <summary>
/// Stream types in FFmpeg used for mapping.
/// </summary>
public enum FFmpegStream {
    /// <summary>
    /// Video stream selection.
    /// </summary>
    Video = 'v',
    /// <summary>
    /// Audio stream selection.
    /// </summary>
    Audio = 'a',
    /// <summary>
    /// Subtitle stream selection.
    /// </summary>
    Subtitle = 's',
}
