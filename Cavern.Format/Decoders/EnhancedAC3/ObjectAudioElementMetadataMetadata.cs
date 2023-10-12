using System.Collections.Generic;

using Cavern.Format.Common;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class OAElementMD : IMetadataSupplier {
        public ReadableMetadata GetMetadata() {
            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader> {
                new ReadableMetadataHeader("Object Audio Element Metadata", new[] {
                    new ReadableMetadataField("sample_offset", "Offset from the frame beginning in samples", sampleOffset),
                    new ReadableMetadataField("num_obj_info_blocks", "Number of block update info blocks", blockOffsetFactor.Length)
                })
            };
            if (infoBlocks != null) {
                for (int obj = 0; obj < updateLast.Length; ++obj) {
                    for (int blk = 0; blk < infoBlocks[obj].Length; ++blk) {
                        headers.AddRange(infoBlocks[obj][blk].GetMetadata().Headers);
                    }
                }
            }
            return new ReadableMetadata(headers);
        }
    }
}