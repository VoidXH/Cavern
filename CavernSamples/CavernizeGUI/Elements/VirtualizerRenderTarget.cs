using Cavern;
using Cavern.Channels;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// Applies a layout for headphone virtualization.
    /// </summary>
    class VirtualizerRenderTarget : RenderTarget {
        /// <summary>
        /// Applies a layout for headphone virtualization.
        /// </summary>
        public VirtualizerRenderTarget() :
            base(targetName, new ReferenceChannel[] { ReferenceChannel.SideLeft, ReferenceChannel.SideRight }) { }

        /// <summary>
        /// Apply this render target on the system's output.
        /// </summary>
        public override void Apply() => Listener.HeadphoneVirtualizer = true;

        /// <summary>
        /// Name of this render target.
        /// </summary>
        const string targetName = "Headphone Virtualizer";
    }
}