using System.Numerics;

using Cavern.Channels;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Constants required for rendering.
    /// </summary>
    public abstract partial class Renderer {
        /// <summary>
        /// Get which standard renderer position corresponds to which channel.
        /// </summary>
        /// <remarks>Internal Cavern channel positions are not the same.</remarks>
        public static ReferenceChannel ChannelFromPosition(Vector3 position) {
            for (int i = 0; i < ChannelPrototype.AlternativePositions.Length; i++) {
                if (position == ChannelPrototype.AlternativePositions[i]) {
                    return (ReferenceChannel)i;
                }
            }
            return ReferenceChannel.Unknown;
        }
    }
}