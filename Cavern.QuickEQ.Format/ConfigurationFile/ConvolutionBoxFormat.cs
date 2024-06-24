using Cavern.Channels;
using Cavern.Filters.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// A file format only supporting matrix mixing and convolution filters.
    /// </summary>
    public sealed class ConvolutionBoxFormat : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => ".cbf";

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Convolution Box Format.
        /// </summary>
        public ConvolutionBoxFormat(ConfigurationFile other) : base(other) { }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public ConvolutionBoxFormat(string name, ReferenceChannel[] inputs) : base(name, inputs) => FinishEmpty(inputs);

        /// <inheritdoc/>
        public override void Export(string path) {
            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            // TODO: file format definition to repo
        }
    }
}