using Cavernize.Logic.CavernSettings;

namespace CavernizeGUI.CavernSettings;

/// <summary>
/// Use the settings stored in Cavernize for special rendering modes.
/// </summary>
sealed class DynamicSpecialRenderModeSettings : SpecialRenderModeSettings {
    /// <summary>
    /// Said settings file.
    /// </summary>
    readonly Resources.Settings source = Resources.Settings.Default;

    /// <inheritdoc/>
    public override bool SpeakerVirtualizer {
        get => source.speakerVirtualizer;
        set => source.speakerVirtualizer = value;
    }
}
