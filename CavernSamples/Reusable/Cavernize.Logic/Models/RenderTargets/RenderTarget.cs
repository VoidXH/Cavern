using System.Numerics;

using Cavern;
using Cavern.Channels;
using Cavern.Utilities;

namespace Cavernize.Logic.Models.RenderTargets;

/// <summary>
/// Standard rendering channel layouts.
/// </summary>
/// <param name="name">Layout name</param>
/// <param name="channels">List of used channels</param>
public partial class RenderTarget(string name, ReferenceChannel[] channels) {
    /// <summary>
    /// Move elevated channels inward by this ratio. 0 is at the sides, 1 is at the center.
    /// </summary>
    public static float HeightSqueeze { get; set; }

    /// <summary>
    /// Layout name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// List of used channels.
    /// </summary>
    public ReferenceChannel[] Channels { get; } = channels;

    /// <summary>
    /// The exact <see cref="OutputChannels"/>, what will have an output.
    /// These are the channels that are not just virtual.
    /// </summary>
    public virtual ReferenceChannel[] WiredChannels => Channels;

    /// <summary>
    /// The <see cref="Channels"/> are used for rendering, but it could be rematrixed.
    /// This is the number of channels actually written to the file.
    /// </summary>
    public int OutputChannels { get; protected set; } = channels.Length;

    /// <summary>
    /// Top rear channels are used as &quot;side&quot; channels as no true rears are available in some standard mappings or in WAVEFORMATEX channel masks.
    /// These have to be mapped back to sides in some cases, for example, for the wiring popup.
    /// </summary>
    protected static ReferenceChannel[] GetNameMappedChannels(ReferenceChannel[] source) {
        ReferenceChannel[] result = source.FastClone();
        bool side = false, rear = false;
        for (int i = 0; i < result.Length; i++) {
            side |= result[i] == ReferenceChannel.TopSideLeft;
            rear |= result[i] == ReferenceChannel.TopRearLeft;
        }
        if (side && rear) {
            return result;
        }

        for (int i = 0; i < result.Length; i++) {
            if (result[i] == ReferenceChannel.TopRearLeft) {
                result[i] = ReferenceChannel.TopSideLeft;
            }
            if (result[i] == ReferenceChannel.TopRearRight) {
                result[i] = ReferenceChannel.TopSideRight;
            }
        }
        return result;
    }

    /// <summary>
    /// Apply this render target on the system's output.
    /// </summary>
    /// <param name="surroundSwap">Swap the side and rear surround outputs</param>
    public virtual void Apply(bool surroundSwap) {
        Channel[] systemChannels = new Channel[Channels.Length];
        for (int ch = 0; ch < Channels.Length; ch++) {
            Vector3 pos = ChannelPrototype.AlternativePositions[(int)Channels[ch]];
            if (HeightSqueeze != 0 && Channels[ch].IsHeight()) {
                systemChannels[ch] = new Channel(new Vector3(
                    QMath.Lerp(pos.X, .5f, HeightSqueeze),
                    pos.Y,
                    QMath.Lerp(pos.Z, .5f, HeightSqueeze)
                ), false);
            } else {
                systemChannels[ch] = new Channel(pos, Channels[ch] == ReferenceChannel.ScreenLFE);
            }
        }

        if (surroundSwap && Channels.Length >= 8) {
            (systemChannels[4], systemChannels[6]) = (systemChannels[6], systemChannels[4]);
            (systemChannels[5], systemChannels[7]) = (systemChannels[7], systemChannels[5]);
        }

        Listener.HeadphoneVirtualizer = false;
        Listener.ReplaceChannels(systemChannels);
    }

    /// <summary>
    /// Top rear channels are used as &quot;side&quot; channels as no true rears are available in standard mappings.
    /// These have to be mapped back to sides in some cases, for example, for the wiring popup.
    /// </summary>
    public ReferenceChannel[] GetNameMappedChannels() => GetNameMappedChannels(WiredChannels);

    /// <summary>
    /// Gets if a channel is actually present in the final file or just used for downmixing in <see cref="DownmixedRenderTarget"/>.
    /// </summary>
    public virtual bool IsExported(int _) => true;

    /// <summary>
    /// Return the <see cref="Name"/> on string conversion.
    /// </summary>
    override public string ToString() => Name;
}
