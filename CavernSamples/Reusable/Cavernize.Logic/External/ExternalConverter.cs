using Cavern.Utilities;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.External;

/// <summary>
/// Convert an unsupported codec to a supported one for rendering.
/// </summary>
/// <param name="language">Database of localized strings</param>
public abstract class ExternalConverter(ExternalConverterStrings language) {
    /// <summary>
    /// Relay text update messages to the UI.
    /// </summary>
    public event Action<string> UpdateStatus;

    /// <summary>
    /// Provides an interface to show the licence of the external converter.
    /// </summary>
    public ILicence LicenceDisplay { get; set; }

    /// <summary>
    /// Where to work with external software.
    /// </summary>
    protected readonly string cavernizeData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cavernize");

    /// <summary>
    /// Database of localized strings.
    /// </summary>
    protected ExternalConverterStrings language = language;

    /// <summary>
    /// This function runs on the UI thread so any required user input can be requested.
    /// </summary>
    public abstract void PrepareOnUI();

    /// <summary>
    /// Convert the <paramref name="source"/> track to a format Cavern can render.
    /// </summary>
    public abstract CavernizeTrack Convert(CavernizeTrack source);

    /// <summary>
    /// Remove the intermediate files after rendering is finished.
    /// </summary>
    public abstract void Cleanup();

    /// <summary>
    /// Update the status message in the UI.
    /// </summary>
    /// <param name="message"></param>
    protected void UpdateStatusMessage(string message) => UpdateStatus?.Invoke(message);
}
