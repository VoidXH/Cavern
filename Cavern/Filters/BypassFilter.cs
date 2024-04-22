namespace Cavern.Filters {
    /// <summary>
    /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
    /// </summary>
    public class BypassFilter : Filter {
        /// <summary>
        /// Name of this filter node.
        /// </summary>
        readonly string name;

        /// <summary>
        /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
        /// </summary>
        /// <param name="name">Name of this filter node</param>
        public BypassFilter(string name) => this.name = name;

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            // Bypass
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            // Bypass
        }

        /// <inheritdoc/>
        public override string ToString() => name;
    }
}