using Cavern;

namespace Cavernize.Logic.CavernSettings;

/// <summary>
/// Holds settings for rendering modifiers regarding a conversion.
/// </summary>
public class RenderingSettings {
    /// <summary>
    /// The voltage gain at which the content is rendered. Shall default to 1.
    /// </summary>
    public virtual float Gain { get; set; } = 1;

    /// <summary>
    /// Virtualize and downmix elevated objects to ground-only layouts by practically applying HRTFs on height channels.
    /// </summary>
    public virtual bool SpeakerVirtualizer { get; set; }

    /// <summary>
    /// Mute <see cref="Source"/>s at reference channel positions.
    /// </summary>
    public virtual bool MuteBed { get; set; }

    /// <summary>
    /// Mute <see cref="Source"/>s which have no elevation.
    /// </summary>
    public virtual bool MuteGround { get; set; }

    /// <summary>
    /// Forces 24-bit export for formats that support it, like WAV or LAF.
    /// </summary>
    public virtual bool Force24Bit { get; set; }
}
