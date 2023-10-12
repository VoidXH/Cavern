using System.Collections.Generic;

using Cavern.Format.Common;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() {
            List<ReadableMetadataHeader> headers = new List<ReadableMetadataHeader> {
                new ReadableMetadataHeader("Joint Object Coding header", new[] {
                    new ReadableMetadataField("joc_num_channels", "Number of channels in the JOC downmix", ChannelCount),
                    new ReadableMetadataField("joc_num_objects", "Number of rendered dynamic objects", ObjectCount),
                    new ReadableMetadataField("joc_clipgain", "Multiplier for the output signal's amplitude", Gain)
                })
            };
            for (int i = 0; i < ObjectCount; i++) {
                headers.Add(new ReadableMetadataHeader("Joint Object Coding object " + (i + 1), new[] {
                    new ReadableMetadataField("b_joc_obj_present", "The object is active", ObjectActive[i]),
                    new ReadableMetadataField("joc_num_bands_idx", "Encoded joc_num_bands", bandsIndex[i]),
                    new ReadableMetadataField("joc_num_bands", "Number of frequency bands for JOC side information", bands[i]),
                    new ReadableMetadataField("b_joc_sparse", "The object is coded in sparse mode", sparseCoded[i]),
                    new ReadableMetadataField("joc_num_quant_idx", "Encoded joc_num_quant", quantizationTable[i]),
                    new ReadableMetadataField("joc_num_quant", "Number of quantization steps for the JOC side information",
                        quantizationTable[i] == 1 ? 192 : 96),
                    new ReadableMetadataField("joc_slope_idx", "JOC interpolation type", steepSlope[i] ? "smooth" : "steep"),
                    new ReadableMetadataField("joc_num_dpoints", "Number of JOC data points", dataPoints[i]),
                    new ReadableMetadataField("(derived)", "Educated guess of this object's JOC bitrate",
                        (sparseCoded[i] ? 1 : ChannelCount) * bands[i] * 3 * 48000 / (1536 * 8) + " bps")
                }));
            }
            return new ReadableMetadata(headers);
        }
    }
}