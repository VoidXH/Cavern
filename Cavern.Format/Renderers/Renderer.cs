using System.Collections.Generic;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded stream with Cavern.
    /// </summary>
    public abstract class Renderer {
        /// <summary>
        /// Rendered spatial objects.
        /// </summary>
        public IReadOnlyList<Source> Objects => objects;

        /// <summary>
        /// Rendered spatial objects.
        /// </summary>
        protected readonly List<Source> objects = new List<Source>();

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the <see cref="objects"/>.
        /// </summary>
        public abstract void Update(int samples);
    }
}