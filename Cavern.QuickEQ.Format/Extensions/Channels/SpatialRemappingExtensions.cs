using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.ConfigurationFile;
using Cavern.Remapping;
using Cavern.Utilities;

using System.Linq;

namespace Cavern.Channels {
    /// <summary>
    /// Operations on <see cref="SpatialRemapping"/> that require Cavern.QuickEQ.Format classes.
    /// </summary>
    public static class SpatialRemappingExtensions {
        /// <summary>
        /// Export a <see cref="SpatialRemapping"/> <paramref name="matrix"/> to a <paramref name="target"/>
        /// <see cref="ConfigurationFile"/>. Export in this case means the remapping is attached in front of the configuration.
        /// </summary>
        /// <exception cref="ChannelCountMismatchException">There are not enough channels in the
        /// <see cref="ConfigurationFile"/> to apply the provided <paramref name="matrix"/>.</exception>
        public static void ToConfigurationFile(MixingMatrix matrix, ConfigurationFile target) {
            if (target.InputChannels.Length < matrix.Length || target.InputChannels.Length < matrix[0].Length) {
                throw new ChannelCountMismatchException();
            }
            target.AddSplitPoint(0, "Spatial remapping");
            FilterGraphNode[] roots = target.InputChannels.GetItem2s();

            FilterGraphNode[] outputs = roots.Select(x => x.Children[0]).ToArray();
            for (int i = 0; i < matrix.Length; i++) {
                float[] gains = matrix[i];
                outputs[i].DetachParents();
                for (int input = 0; input < gains.Length; input++) {
                    if (gains[input] == 1) {
                        roots[input].AddChild(outputs[i]);
                    } else if (gains[input] != 0) {
                        Gain mix = new Gain(QMath.GainToDb(gains[input]));
                        FilterGraphNode middleNode = roots[input].AddChild(mix);
                        middleNode.AddChild(outputs[i]);
                    }
                }
            }
        }
    }
}
