using Cavern.Channels;
using Cavern.Utilities;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A render target that renders for a different layout than what's written to the file.
    /// This layout is achieved through merging some of the rendered channels together.
    /// Mainly used for X.X.2 front exports, in those cases, the rears are mixed to the ground to provide elevation only at the screen.
    /// </summary>
    /// <remarks>Only merge the last channels to any other, as those can be efficiently left out while exporting, and only the
    /// first <see cref="OutputChannels"/> will be kept.</remarks>
    class DownmixedRenderTarget : RenderTarget {
        /// <summary>
        /// Channels pairs in the form of (a, b) where a is mixed into b and then thrown away.
        /// </summary>
        readonly (int source, int target)[] merge;

        /// <summary>
        /// A render target that renders for a different layout than what's written to the file.
        /// This layout is achieved through merging some of the rendered channels together.
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="channels">Channels that are used for rendering</param>
        /// <param name="merge">Channels pairs in the form of (a, b) where a is mixed into b and then thrown away</param>
        /// <remarks>Only merge the last channels to any other, as those can be efficiently left out while exporting, and only the
        /// first <see cref="OutputChannels"/> will be kept.</remarks>
        public DownmixedRenderTarget(string name, ReferenceChannel[] channels, params (int, int)[] merge) :
            base(name, channels) {
            this.merge = merge;
            OutputChannels = channels.Length - merge.Length;
        }

        /// <summary>
        /// Perform the channel mixing by the <see cref="merge"/> mapping.
        /// </summary>
        public void PerformMerge(float[] samples) {
            for (int i = 0; i < merge.Length; i++) {
                WaveformUtils.Mix(samples, merge[i].source, merge[i].target, Channels.Length);
            }
        }
    }
}