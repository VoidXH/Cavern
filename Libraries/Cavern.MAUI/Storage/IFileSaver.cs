namespace Cavern.MAUI.Storage;

/// <summary>
/// Saves a <see cref="Stream"/> to a file.
/// </summary>
public interface IFileSaver {
    /// <summary>
    /// Copy <paramref name="data"/> to a file created where the user wants it.
    /// </summary>
    Task<bool> SaveFileAsync(byte[] data, string defaultFileName);

    /// <summary>
    /// Copy a <paramref name="stream"/> to a file created where the user wants it.
    /// </summary>
    Task<bool> SaveFileAsync(Stream stream, string defaultFileName);
}
