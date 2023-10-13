using System.Collections.Generic;
using System.Linq;

using Cavern.Format.Common;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class OAElementMD : IMetadataSupplier {
        /// <inheritdoc/>
        public ReadableMetadata GetMetadata() {
            if (blockOffsetFactor[0] < 0) {
                return new ReadableMetadata(new[] {
                    new ReadableMetadataHeader("Object Audio Element Metadata (Unknown Element)", new[] {
                        new ReadableMetadataField("oa_element_id_idx", "Type of the Object Audio Element", -1 - blockOffsetFactor[0]),
                    })
                });
            }

            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader> {
                new ReadableMetadataHeader("Object Audio Element Metadata (Object Element)", new[] {
                    new ReadableMetadataField("sample_offset", "Offset from the frame beginning in samples", sampleOffset),
                    new ReadableMetadataField("num_obj_info_blocks", "Number of block update info blocks", blockOffsetFactor.Length)
                })
            };

            int channels = 0;
            for (int obj = 0; obj < updateLast.Length; ++obj) {
                for (int blk = 0; blk < infoBlocks[obj].Length; ++blk) {
                    ObjectInfoBlock block = infoBlocks[obj][blk];
                    string title = block.IsBed ?
                        $"Bed {++channels} Info Block {blk + 1}" :
                        $"Object {obj - channels + 1} Info Block {blk + 1}";
                    headers.AddRange(block.GetMetadata().Headers.Select(x => new ReadableMetadataHeader(title, x.Fields)));
                }
            }
            return new ReadableMetadata(headers);
        }
    }
}