using Cavern.Channels;

namespace Cavern.Format.Environment.Utilities {
    /// <summary>
    /// A moving audio <see cref="Source"/> ("object") anchored to a physical channel position.
    /// </summary>
    public readonly struct StaticSource {
        /// <summary>
        /// The fixed location of the <see cref="Source"/>.
        /// </summary>
        public ReferenceChannel Channel { get; }

        /// <summary>
        /// Renderer of the audio anchored to the <see cref="Channel"/>.
        /// </summary>
        public Source Source { get; }

        /// <summary>
        /// A moving audio <see cref="Source"/> ("object") anchored to a physical channel position.
        /// </summary>
        /// <param name="channel">The fixed location of the <paramref name="source"/></param>
        /// <param name="source">Renderer of the audio anchored to the <paramref name="channel"/></param>
        public StaticSource(ReferenceChannel channel, Source source) {
            Channel = channel;
            Source = source;
        }
    }
}
