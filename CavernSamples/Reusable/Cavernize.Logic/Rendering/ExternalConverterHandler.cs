using Cavern;
using Cavern.CavernSettings;
using Cavern.Format.Common;
using Cavern.Utilities;

using Cavernize.Logic.External;
using Cavernize.Logic.Language;
using Cavernize.Logic.Models;

namespace Cavernize.Logic.Rendering;

/// <summary>
/// Instantiates and disposes external converters when needed. Recreates the <see cref="CavernizeTrack"/> if needed.
/// </summary>
public sealed class ExternalConverterHandler : IDisposable {
    /// <summary>
    /// Conversion must happen externally, as the format is not supported by Cavern.
    /// </summary>
    public bool ExternalConverterNeeded => handler != null;

    /// <summary>
    /// The setup or the conversion failed, the intermediate file won't be available or in integrity.
    /// </summary>
    public bool Failed { get; private set; }

    /// <summary>
    /// The external converter required for the current process.
    /// </summary>
    readonly ExternalConverter handler;

    /// <summary>
    /// Render this supported format that was created with the external converter instead of the unsupported track.
    /// </summary>
    readonly CavernizeTrack intermediateTrack;

    /// <summary>
    /// Instantiates and disposes external converters when needed. Recreates the <see cref="CavernizeTrack"/> if needed.
    /// </summary>
    /// <param name="target">The audio track we want to convert but in an unsupported format</param>
    /// <param name="language">Localization of external converter statuses</param>
    /// <param name="licenceHandler">Displays a licence agreement prompt or window to the user</param>
    /// <param name="updateProgress">Called with the ratio of progression, with -1 being a possibility for undetermined progress</param>
    /// <param name="updateStatus">Called with a text description of the current operation</param>
    /// <param name="mainThread">A function that runs code on the main thread in order to pause application operation to ask for a licence agreement</param>
    public ExternalConverterHandler(CavernizeTrack target, ExternalConverterStrings language, ILicence licenceHandler,
        Action<double> updateProgress, Action<string> updateStatus, Action<Action> mainThread) {
        if (target.Codec == Codec.TrueHD) { // Use truehdd if needed
            handler = new Truehdd(language);
        }
        if (handler != null) {
            updateProgress?.Invoke(-1);
            handler.UpdateStatus += updateStatus;
            try {
                mainThread(() => {
                    handler.LicenceDisplay = licenceHandler;
                    handler.PrepareOnUI();
                });
                intermediateTrack = handler.Convert(target);
            } catch (Exception e) {
                updateStatus?.Invoke(e.Message);
                updateProgress?.Invoke(1);
                Failed = true;
            }
        }
    }

    /// <summary>
    /// If external conversion is needed, attach the <see cref="intermediateTrack"/> to a <see cref="Listener"/> for rendering.
    /// </summary>
    public void Attach(Listener to, UpmixingSettings upmixing) {
        if (!ExternalConverterNeeded) {
            return;
        }
        to.DetachAllSources();
        intermediateTrack.Attach(to, upmixing);
    }

    /// <inheritdoc/>
    public void Dispose() => handler?.Cleanup();
}
