using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live Bass Control.
    /// The difference between normal Dirac Live is this one requires a combined curve for some channels.
    /// </summary>
    public class DiracLiveBassControlFilterSet : DiracLiveFilterSet {
        /// <summary>
        /// Some versions of DLBC combine all heights to a single group. This option creates such an export.
        /// </summary>
        public bool CombineHeights { get; set; }

        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live Bass Control.
        /// </summary>
        public DiracLiveBassControlFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// A filter set exporting raw <see cref="Equalizer"/>s for Dirac Live Bass Control.
        /// </summary>
        public DiracLiveBassControlFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Save the results to EQ curve files for each channel.
        /// </summary>
        public override void Export(string path) {
            CreateRootFile(path, "txt");
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            (string name, ReferenceChannel[] channels)[] channelGroups =
                CombineHeights ? groupsWithCombinedHeights : groupsWithSeparateHeights;
            List<Equalizer>[] groups = new List<Equalizer>[channelGroups.Length];
            List<(Equalizer curve, string name)> standalones = new List<(Equalizer, string)>();
            for (int i = 0; i < Channels.Length; i++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[i];
                bool found = false;
                for (int j = 0; j < channelGroups.Length; j++) {
                    if (channelGroups[j].channels.Contains(channelRef.reference)) {
                        if (groups[j] == null) {
                            groups[j] = new List<Equalizer>();
                        }
                        groups[j].Add(channelRef.curve);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    standalones.Add((channelRef.curve, channelRef.name));
                }
            }

            for (int i = 0; i < channelGroups.Length; i++) {
                if (groups[i] != null) {
                    EQGenerator.AverageSafe(groups[i].ToArray()).ExportToDirac(
                        Path.Combine(folder, $"{fileNameBase} {channelGroups[i].name}.txt"), 0, optionalHeader);
                }
            }
            for (int i = 0, c = standalones.Count; i < c; i++) {
                standalones[i].curve.ExportToDirac(Path.Combine(folder, $"{fileNameBase} {standalones[i].name}.txt"), 0, optionalHeader);
            }
        }

        /// <summary>
        /// The channels that are combined into one EQ group for DLBC versions where the height pairs are separate groups.
        /// </summary>
        static readonly (string name, ReferenceChannel[] channels)[] groupsWithSeparateHeights = {
            ("Fronts", new[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight }),
            ("Wide Surrounds", new[] { ReferenceChannel.WideLeft, ReferenceChannel.WideRight }),
            ("Side Surrounds", new[] { ReferenceChannel.SideLeft, ReferenceChannel.SideRight }),
            ("Rear Surrounds", new[] { ReferenceChannel.RearLeft, ReferenceChannel.RearRight }),
            ("Front Heights", new[] { ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontRight }),
            ("Side Heights", new[] { ReferenceChannel.TopSideLeft, ReferenceChannel.TopSideRight }),
            ("Rear Heights", new[] { ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearRight })
        };

        /// <summary>
        /// The channels that are combined into one EQ group for DLBC versions where there are no different height pair groups.
        /// </summary>
        static readonly (string name, ReferenceChannel[] channels)[] groupsWithCombinedHeights = {
            ("Fronts", new[] { ReferenceChannel.FrontLeft, ReferenceChannel.FrontRight }),
            ("Wide Surrounds", new[] { ReferenceChannel.WideLeft, ReferenceChannel.WideRight }),
            ("Side Surrounds", new[] { ReferenceChannel.SideLeft, ReferenceChannel.SideRight }),
            ("Rear Surrounds", new[] { ReferenceChannel.RearLeft, ReferenceChannel.RearRight }),
            ("Heights", new[] { ReferenceChannel.TopFrontLeft, ReferenceChannel.TopFrontCenter, ReferenceChannel.TopFrontRight,
                ReferenceChannel.TopSideLeft, ReferenceChannel.GodsVoice, ReferenceChannel.TopSideRight,
                ReferenceChannel.TopRearLeft, ReferenceChannel.TopRearCenter, ReferenceChannel.TopRearRight }),
        };
    }
}