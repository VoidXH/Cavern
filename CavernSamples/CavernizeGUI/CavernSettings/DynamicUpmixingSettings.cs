using Cavern.CavernSettings;

namespace CavernizeGUI.CavernSettings;

/// <summary>
/// Use the settings stored in Cavernize for upmixing.
/// </summary>
sealed class DynamicUpmixingSettings : UpmixingSettings {
    /// <summary>
    /// Said settings file.
    /// </summary>
    readonly Resources.UpmixingSettings source = Resources.UpmixingSettings.Default;

    /// <inheritdoc/>
    public override bool MatrixUpmixing {
        get => source.MatrixUpmix;
        set => source.MatrixUpmix = value;
    }

    /// <inheritdoc/>
    public override bool Cavernize {
        get => source.Cavernize;
        set => source.Cavernize = value;
    }

    /// <inheritdoc/>
    public override float Effect {
        get => source.Effect;
        set => source.Effect = value;
    }

    /// <inheritdoc/>
    public override float Smoothness {
        get => source.Smoothness;
        set => source.Smoothness = value;
    }

    /// <summary>
    /// Use the settings stored in Cavernize for upmixing.
    /// </summary>
    public DynamicUpmixingSettings() : base(false) { }
}
