using Cavern;
using Cavern.Remapping;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// Applies the layout that's set up in the Cavern Driver.
    /// </summary>
    class DriverRenderTarget : RenderTarget {
        /// <summary>
        /// Applies the layout that's set up in the Cavern Driver.
        /// </summary>
        public DriverRenderTarget() : base(targetName, new ReferenceChannel[GetChannels()]) { }

        /// <summary>
        /// Gets the channel count set up in the Cavern Driver.
        /// </summary>
        static int GetChannels() {
            new Listener();
            return Listener.Channels.Length;
        }

        /// <summary>
        /// Apply this render target on the system's output.
        /// </summary>
        public override void Apply() => new Listener();

        /// <summary>
        /// Name of this render target.
        /// </summary>
        const string targetName = "Cavern Driver";
    }
}