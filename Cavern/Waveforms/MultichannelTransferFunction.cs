using Cavern.Utilities;

namespace Cavern.Waveforms {
    /// <summary>
    /// Contains multiple transfer functions of the same length.
    /// </summary>
    public class MultichannelTransferFunction : MultichannelBase<Complex> {
        /// <summary>
        /// Encapsulate the transfer function of multiple channels.
        /// </summary>
        public MultichannelTransferFunction(params Complex[][] source) : base(source) { }

        /// <summary>
        /// Construct an empty transfer function set of a given size.
        /// </summary>
        public MultichannelTransferFunction(int channels, int binsPerChannel) : base(channels, binsPerChannel) { }

        /// <inheritdoc/>
        public override object Clone() => new MultichannelTransferFunction(data.DeepCopy2D());
    }
}
