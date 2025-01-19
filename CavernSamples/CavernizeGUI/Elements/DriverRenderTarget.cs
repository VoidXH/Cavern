using Cavern;
using Cavern.Channels;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// Applies the layout that's set up in the Cavern Driver.
    /// </summary>
    class DriverRenderTarget : RenderTarget {
        /// <summary>
        /// Applies the layout that's set up in the Cavern Driver.
        /// </summary>
        public DriverRenderTarget() : base(targetName, GetChannels()) { }

        /// <summary>
        /// Gets the channels set up in the Cavern Driver.
        /// </summary>
        static ReferenceChannel[] GetChannels() {
            new Listener();
            ReferenceChannel[] result = new ReferenceChannel[Listener.Channels.Length];
            for (int i = 0; i < result.Length; i++) {
                result[i] = ChannelPrototype.GetReference(Listener.Channels[i]);
            }
            return result;
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