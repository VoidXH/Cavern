using Cavern.Channels;

namespace Cavern.Format.Renderers.BaseClasses {
    /// <summary>
    /// A <see cref="Renderer"/> that has static beds and dynamic objects in the same stream.
    /// </summary>
    public interface IMixedBedObjectRenderer {
        /// <summary>
        /// Get the &quot;objects&quot; that are just static channels.
        /// </summary>
        ReferenceChannel[] GetStaticChannels();
    }
}
