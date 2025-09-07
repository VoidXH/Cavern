using Cavern;
using Cavern.Utilities;
using Cavern.Virtualizer;

using Cavernize.Logic.Models;
using Cavernize.Logic.Models.RenderTargets;

namespace Cavernize.Logic.Rendering;

/// <summary>
/// Handles main steps of the Cavernize process while being extensible.
/// </summary>
public sealed class ConversionEnvironment {
    /// <summary>
    /// Playback environment used for rendering.
    /// </summary>
    public Listener Listener { get; private set; }

    /// <summary>
    /// Convolution filter for each corresponding channel index to be applied on rendering.
    /// </summary>
    public Clip RoomCorrection { get; set; }

    /// <summary>
    /// The <see cref="roomCorrection"/> is active and the loaded filter set is valid for the currently set output layout.
    /// </summary>
    public bool RoomCorrectionUsed => RoomCorrection != null && Listener.Channels.Length == RoomCorrection.Channels;

    /// <summary>
    /// Keeper of loaded files and settings.
    /// </summary>
    readonly ICavernizeApp app;

    /// <summary>
    /// Handles main steps of the Cavernize process while being extensible.
    /// </summary>
    /// <param name="app">Application implementation keeping</param>
    public ConversionEnvironment(ICavernizeApp app) {
        this.app = app;
        Listener = new() { // Create a listener, which triggers the loading of saved environment settings
            UpdateRate = 64,
            AudioQuality = QualityModes.Perfect
        };
    }

    /// <summary>
    /// Set up the listening environment for playing back the specified track.
    /// </summary>
    public void AttachToListener(CavernizeTrack target) {
        app.RenderTarget.Apply(app.SurroundSwap);
        if (app.RenderTarget is not VirtualizerRenderTarget && app.ExportFormat.MaxChannels < Listener.Channels.Length) {
            throw new OverMaxChannelsException(Listener.Channels.Length, app.ExportFormat.MaxChannels);
        }

        if (app.SpecialRenderModeSettings.SpeakerVirtualizer) {
            VirtualizerFilter.SetupForSpeakers();
            Listener.SampleRate = VirtualizerFilter.FilterSampleRate;
        }

        Reset();
        target.Attach(Listener, app.UpmixingSettings);

        // Prevent height limiting, require at least 4 overhead channels for full gain
        float safeGain = target.Renderer.HasObjects && Listener.Channels.GetOverheadChannelCount() < 4 ? .707f : 1;
        Listener.Volume = app.RenderGain * safeGain;

        if (app.RenderTarget is VirtualizerRenderTarget) {
            if (RoomCorrection != null && RoomCorrection.SampleRate != VirtualizerFilter.FilterSampleRate) {
                throw new SampleRateMismatchException();
            }
            Listener.SampleRate = VirtualizerFilter.FilterSampleRate;
        } else {
            Listener.SampleRate = RoomCorrectionUsed ? target.SampleRate : RoomCorrection.SampleRate;
        }
    }

    /// <summary>
    /// Reset the environment, prepare for next attachment.
    /// </summary>
    public void Reset() {
        Listener.DetachAllSources();
    }
}
