using System.Windows.Controls;

using Cavernize.Logic.CavernSettings;

namespace CavernizeGUI.CavernSettings;

/// <summary>
/// Use the settings stored in Cavernize for special rendering modes.
/// </summary>
sealed class DynamicSpecialRenderModeSettings(MenuItem muteBed, MenuItem muteGround) : SpecialRenderModeSettings {
    /// <summary>
    /// Said settings file.
    /// </summary>
    readonly Resources.Settings source = Resources.Settings.Default;

    /// <inheritdoc/>
    public override bool MuteBed {
        get => muteBed.IsChecked;
        set => muteBed.IsChecked = value;
    }

    /// <inheritdoc/>
    public override bool MuteGround {
        get => muteGround.IsChecked;
        set => muteGround.IsChecked = value;
    }

    /// <inheritdoc/>
    public override bool SpeakerVirtualizer {
        get => source.speakerVirtualizer;
        set => source.speakerVirtualizer = value;
    }
}
