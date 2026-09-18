using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.JSON;

namespace Cavern.Format.FilterSet.Consts {
    /// <summary>
    /// Helper functions MQX-related classes can use.
    /// </summary>
    public static class MQXHelpers {
        /// <summary>
        /// Gets the <see cref="ReferenceChannel"/> for a given <paramref name="designation"/>.
        /// </summary>
        public static ReferenceChannel DesignationToChannel(string designation) {
            for (int i = 0; i < MQXConsts.labeling.Length; i++) {
                if (MQXConsts.labeling[i].designation == designation) {
                    return MQXConsts.labeling[i].channel;
                }
            }
            return ReferenceChannel.Unknown;
        }

        /// <summary>
        /// Get what GUID maps to which actual channel in the MQX file or null if no such mapping is present.
        /// </summary>
        public static Dictionary<string, ReferenceChannel> GetChannelMapping(JsonFile mqx) {
            if (!mqx.ContainsKey(MQXConsts.channelMappingKey)) {
                return null;
            }

            const string metadataKey = "Metadata";
            const string designationKey = "AvrOriginatingDesignation";
            JsonFile mapping = (JsonFile)mqx[MQXConsts.channelMappingKey];

            Dictionary<string, ReferenceChannel> result = new Dictionary<string, ReferenceChannel>();
            foreach (KeyValuePair<string, object> channel in mapping) {
                JsonFile metadata = (JsonFile)((JsonFile)channel.Value)[metadataKey];
                ReferenceChannel reference = DesignationToChannel((string)metadata[designationKey]);
                result[channel.Key] = reference;
            }
            return result;
        }
    }
}
