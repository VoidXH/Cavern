using Cavern.Format.Common;

using System.Collections.Generic;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class ObjectAudioMetadata : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() {
            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader> {
                new ReadableMetadataHeader("Object Audio MetaData header", new[] {
                    new ReadableMetadataField("object_count", "Number of rendered dynamic objects", ObjectCount),
                    new ReadableMetadataField("oa_element_count", "Number of Object Audio Element blocks", elements.Length)
                })
            };
            for (int i = 0; i < elements.Length; i++) {
                headers.AddRange(elements[i].GetMetadata().Headers);
            }
            return new ReadableMetadata(headers);
        }
    }
}