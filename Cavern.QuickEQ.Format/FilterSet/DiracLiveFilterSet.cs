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

        /// <inheritdoc/>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < Channels.Length; i++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[i];
                ExportChannel(Path.Combine(folder, $"{fileNameBase} {channelRef.name}.txt"), channelRef.curve);
            }
        }

        /// <summary>
        /// Write a single channel's <paramref name="eq"/> to a predetermined <paramref name="path"/>.
        /// </summary>
        protected virtual void ExportChannel(string path, Equalizer eq) =>
            eq.ExportToDirac(path, 0, optionalHeader);
    }
}
