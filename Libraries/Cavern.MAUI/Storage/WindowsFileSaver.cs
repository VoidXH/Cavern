#if WINDOWS
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Cavern.MAUI.Storage;

/// <summary>
/// Saves a <see cref="Stream"/> to a file on Windows.
/// </summary>
public class WindowsFileSaver : IFileSaver {
    /// <summary>
    /// Pick a file for writing.
    /// </summary>
    static async Task<Stream> GetFileStream(string defaultFileName) {
        FileSavePicker savePicker = new FileSavePicker();
        IntPtr hwnd = GetActiveWindow();
        InitializeWithWindow.Initialize(savePicker, hwnd);

        string extension = Path.GetExtension(defaultFileName);
        savePicker.FileTypeChoices.Add(extension[1..].ToUpperInvariant() + " file", [extension]);
        savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
        savePicker.SuggestedFileName = defaultFileName;

        StorageFile file = await savePicker.PickSaveFileAsync();
        if (file == null) {
            return null; // User cancelled
        }

        return await file.OpenStreamForWriteAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> SaveFileAsync(byte[] data, string defaultFileName) {
        Stream fileStream = await GetFileStream(defaultFileName);
        if (fileStream == null) {
            return false;
        }

        fileStream.Write(data, 0, data.Length);
        await fileStream.FlushAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SaveFileAsync(Stream stream, string defaultFileName) {
        Stream fileStream = await GetFileStream(defaultFileName);
        if (fileStream == null) {
            return false;
        }

        stream.Position = 0;
        await stream.CopyToAsync(fileStream);
        await fileStream.FlushAsync();
        return true;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();
}
#endif
