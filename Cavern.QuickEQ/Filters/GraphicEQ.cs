using System.Globalization;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO to a Cavern <see cref="GraphicEQ"/> filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphicEQ FromEqualizerAPO(string line, int sampleRate) =>
            FromEqualizerAPO(line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries), sampleRate);

        /// <summary>
        /// Parse a Graphic EQ line of Equalizer APO which was split at spaces to a Cavern <see cref="GraphicEQ"/> filter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphicEQ FromEqualizerAPO(string[] splitLine, int sampleRate) =>
            new GraphicEQ(EQGenerator.FromEqualizerAPO(splitLine), sampleRate);

        /// <inheritdoc/>
        public override string ToString() {
            double roundedPeak = (int)(Equalizer.PeakGain * 100 + .5) * .01;
            return $"Graphic EQ: {Equalizer.Bands.Count} bands, {roundedPeak.ToString(CultureInfo.InvariantCulture)} dB peak";
        }
    }
}