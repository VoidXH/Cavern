namespace Cavern.Format.ConfigurationFile.Presets {
    /// <summary>
    /// An added preconfigured step to a <see cref="ConfigurationFile"/> filter graph.
    /// </summary>
    public abstract class FilterSetPreset {
        /// <summary>
        /// Add this preset to a work in progress configuration <paramref name="file"/> at the given split point <paramref name="index"/>.
        /// </summary>
        public abstract void Add(ConfigurationFile file, int index);
    }
}