using Cavern;

namespace Cavernize.Logic.CavernSettings;

/// <summary>
/// Holds settings for rendering modifiers regarding a conversion.
/// </summary>
public class SpecialRenderModeSettings {
    /// <summary>
    /// Mute <see cref="Source"/>s at reference channel positions.
    /// </summary>
    public virtual bool MuteBed { get; set; }

    /// <summary>
    /// Mute <see cref="Source"/>s which have no elevation.
    /// </summary>
    public virtual bool MuteGround { get; set; }

    /// <summary>
    /// Virtualize and downmix elevated objects to ground-only layouts by practically applying HRTFs on height channels.
    /// </summary>
    public virtual bool SpeakerVirtualizer { get; set; }
}
