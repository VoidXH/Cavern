using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live.
    /// </summary>
    public class DiracLiveFilterSet : EqualizerFilterSet {
        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live.
        /// </summary>
        public DiracLiveFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live.
        /// </summary>
        public DiracLiveFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Save the results to EQ curve files for each channel.
        /// </summary>
        public override void Export(string path) {
            CreateRootFile(path, "txt");
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            for (int i = 0; i < Channels.Length; i++) {
                var channelRef = (EqualizerChannelData)Channels[i];
                channelRef.curve.ExportToDirac(Path.Combine(folder, $"{fileNameBase} {channelRef.name}.txt"), 0, optionalHeader);
            }
        }
    }
}