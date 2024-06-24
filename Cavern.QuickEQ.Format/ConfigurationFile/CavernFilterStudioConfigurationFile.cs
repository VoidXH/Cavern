using Cavern.Channels;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Cavern Filter Studio's own export format for full grouped filter pipelines.
    /// </summary>
    public sealed class CavernFilterStudioConfigurationFile : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => ".cfs";

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Cavern's format.
        /// </summary>
        public CavernFilterStudioConfigurationFile(ConfigurationFile other) : base(other) { }

        /// <summary>
        /// Create an empty file for a standard layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, int channelCount) :
            this(name, ChannelPrototype.GetStandardMatrix(channelCount)) { }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public CavernFilterStudioConfigurationFile(string name, params ReferenceChannel[] channels) : base(name, channels) =>
            FinishEmpty(channels);

        /// <inheritdoc/>
        public override void Export(string path) {
            throw new System.NotImplementedException();
        }
    }
}