using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Equalizer filter set for the Wavelet Android app.
    /// </summary>
    public class WaveletFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Equalizer filter set for the Wavelet Android app.
        /// </summary>
        public WaveletFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Equalizer filter set for the Wavelet Android app.
        /// </summary>
        public WaveletFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) {
            Equalizer[] sources = Channels
                .Where(x => x.reference != ReferenceChannel.ScreenLFE)
                .Select(x => ((EqualizerChannelData)x).curve)
                .ToArray();
            string result;
            try {
                result = EQGenerator.AverageRMS(sources).ExportToEqualizerAPO();
            } catch {
                result = EQGenerator.AverageSafe(sources).ExportToEqualizerAPO();
            }
            File.WriteAllText(path, result);
        }
    }
}