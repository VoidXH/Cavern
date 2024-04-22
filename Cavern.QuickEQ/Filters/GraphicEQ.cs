using Cavern.QuickEQ.Equalization;

namespace Cavern.Filters {
    /// <summary>
    /// Converts an <see cref="Equalizer"/> to a convolution filter.
    /// </summary>
    /// <remarks>This filter is part of the Cavern.QuickEQ library and is not available in the Cavern library's Filters namespace,
    /// because it requires QuickEQ library functions.</remarks>
    public class GraphicEQ : FastConvolver {
        /// <summary>
        /// Copy of the equalizer curve for further alignment.
        /// </summary>
        /// <remarks>Changing the bands on this <see cref="Equalizer"/> does not result in the recomputation of the convolution filter,
        /// please recreate the filter instead.</remarks>
        public Equalizer Equalizer { get; }

        /// <summary>
        /// Convert an <paramref name="equalizer"/> to a 65536-sample convolution filter.
        /// </summary>
        /// <param name="equalizer">Desired frequency response change</param>
        /// <param name="sampleRate">Sample rate of the filter</param>
        public GraphicEQ(Equalizer equalizer, int sampleRate) : this(equalizer, sampleRate, 65536) { }

        /// <summary>
        /// Convert an <paramref name="equalizer"/> to a convolution filter.
        /// </summary>
        /// <param name="equalizer">Desired frequency response change</param>
        /// <param name="sampleRate">Sample rate of the filter</param>
        /// <param name="filterLength">Number of samples in the generated convolution filter, must be a power of 2</param>
        public GraphicEQ(Equalizer equalizer, int sampleRate, int filterLength) :
            base(equalizer.GetConvolution(sampleRate, filterLength)) => Equalizer = equalizer;

        /// <inheritdoc/>
        public override string ToString() {
            double roundedPeak = (int)(Equalizer.PeakGain * 100 + .5) * .01;
            return $"Graphic EQ: {Equalizer.Bands.Count} bands, {roundedPeak} dB peak";
        }
    }
}