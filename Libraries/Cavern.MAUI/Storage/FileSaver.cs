namespace Cavern.MAUI.Storage;

/// <summary>
/// Platform-independent file saving utility.
/// </summary>
public class FileSaver : IFileSaver {
    /// <summary>
    /// The underlying instance for handling OS-dependent file operations.
    /// </summary>
    readonly IFileSaver saver;

    /// <summary>
    /// Platform-independent file saving utility.
    /// </summary>
    public FileSaver() {
#if WINDOWS
        saver = new WindowsFileSaver();
#else
        throw new PlatformNotSupportedException();
#endif
    }

    /// <inheritdoc/>
    public Task<bool> SaveFileAsync(byte[] data, string defaultFileName) => saver.SaveFileAsync(data, defaultFileName);

    /// <inheritdoc/>
    public Task<bool> SaveFileAsync(Stream stream, string defaultFileName) => saver.SaveFileAsync(stream, defaultFileName);
}
