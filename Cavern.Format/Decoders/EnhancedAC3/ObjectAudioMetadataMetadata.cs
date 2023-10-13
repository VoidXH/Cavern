using Cavern.Format.Common;

using System.Collections.Generic;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class ObjectAudioMetadata : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() {
            bool hasLFE = bedAssignment.Length == 1 && bedAssignment[0][(int)NonStandardBedChannel.LowFrequencyEffects];
            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader> {
                new ReadableMetadataHeader("Object Audio MetaData header", new[] {
                    new ReadableMetadataField("object_count", "Total number of audio objects, including static and dynamic", ObjectCount),
                    new ReadableMetadataField("oa_element_count", "Number of Object Audio Element blocks", elements.Length),
                    new ReadableMetadataField("b_dyn_object_only_program", "The program only contains dynamic objects other than the LFE",
                        bedAssignment.Length == 0 || hasLFE),
                    new ReadableMetadataField("b_lfe_present", "The LFE channel is present in an object-only program", hasLFE),
                    new ReadableMetadataField("num_bed_instances", "Number of bed channels or channel pairs", bedAssignment.Length),
                    new ReadableMetadataField("num_dynamic_objects", "Number of dynamic objects", ObjectCount - bedAssignment.Length),
                })
            };
            for (int i = 0; i < elements.Length; i++) {
                headers.AddRange(elements[i].GetMetadata().Headers);
            }
            return new ReadableMetadata(headers);
        }
    }
}