using System.IO;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for MiniDSP Tide16.
    /// </summary>
    /// <remarks>MiniDSP Tide16 only works on 48 kHz sampling rate. Using anything else breaks the filter set.</remarks>
    public class MiniDSPTide16FilterSet : MiniDSP2x4FilterSet {
        /// <summary>
        /// IIR filter set for MiniDSP Tide16.
        /// </summary>
        public MiniDSPTide16FilterSet(int channels) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for MiniDSP Tide16 with a given set of channels.
        /// </summary>
        public MiniDSPTide16FilterSet(ReferenceChannel[] channels) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);
            CreateRootFile(path, "txt");

            for (int i = 0; i < Channels.Length; i++) {
                string channelPath = Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt");
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                SaveFilters(filters, 0, Bands >> 1, channelPath);
            }
        }

        /// <summary>
        /// Fixed sample rate of the Tide16.
        /// </summary>
        const int sampleRate = 48000;
    }
}
