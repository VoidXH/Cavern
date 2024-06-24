namespace Cavern.Filters {
    /// <summary>
    /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
    /// </summary>
    public class BypassFilter : Filter {
        /// <summary>
        /// Name of this filter node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A filter that doesn't do anything. Used to display empty filter nodes with custom names, like the beginning of virtual channels.
        /// </summary>
        /// <param name="name">Name of this filter node</param>
        public BypassFilter(string name) => Name = name;

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            // Bypass
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            // Bypass
        }

        /// <inheritdoc/>
        public override object Clone() => new BypassFilter(Name);

        /// <inheritdoc/>
        public override string ToString() => Name;
    }
}