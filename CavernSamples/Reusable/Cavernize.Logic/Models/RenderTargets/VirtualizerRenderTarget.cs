using Cavern;
using Cavern.Channels;

namespace Cavernize.Logic.Models.RenderTargets;

/// <summary>
/// Applies a layout for headphone virtualization.
/// </summary>
public class VirtualizerRenderTarget : RenderTarget {
    /// <summary>
    /// Applies a layout for headphone virtualization.
    /// </summary>
    public VirtualizerRenderTarget() : base(targetName, [ReferenceChannel.SideLeft, ReferenceChannel.SideRight]) => OutputChannels = 2;

    /// <inheritdoc/>
    public override void Apply(bool _) => Listener.HeadphoneVirtualizer = true;

    /// <summary>
    /// Name of this render target.
    /// </summary>
    const string targetName = "Headphone Virtualizer";
}
