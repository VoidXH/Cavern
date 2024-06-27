using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.FilterSet;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// A file format only supporting matrix mixing and convolution filters.
    /// </summary>
    public sealed class ConvolutionBoxFormatConfigurationFile : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => "cbf";

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Convolution Box Format with the default convoltion length of
        /// 65536 samples.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(ConfigurationFile other) : this(other, 65536) { }

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Convolution Box Format with a custom
        /// <paramref name="convolutionLength"/>.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(ConfigurationFile other, int convolutionLength) : base(other) {
            MergeSplitPoints();
            Optimize();
            SplitPoints[0].roots.ConvertToConvolution(convolutionLength);
        }

        /// <summary>
        /// Create an empty file for a custom layout.
        /// </summary>
        public ConvolutionBoxFormatConfigurationFile(string name, ReferenceChannel[] inputs) : base(name, inputs) => FinishEmpty(inputs);

        /// <inheritdoc/>
        public override void Export(string path) {
            ValidateForExport();
            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            // TODO: file format definition to repo
        }

        /// <summary>
        /// Throw an <see cref="UnsupportedFilterException"/> if it's not a convolution or a merge point.
        /// </summary>
        void ValidateForExport() {
            foreach (FilterGraphNode node in SplitPoints[0].roots.MapGraph()) {
                if (!(node.Filter is BypassFilter) && !(node.Filter is Convolver) && !(node.Filter is FastConvolver)) {
                    throw new UnsupportedFilterException(node.Filter);
                }
            }
        }

        /// <summary>
        /// CBFM marker.
        /// </summary>
        const int magicNumber = 0x4D464243;
    }
}