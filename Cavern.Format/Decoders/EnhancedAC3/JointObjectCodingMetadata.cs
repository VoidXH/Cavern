using Cavern.Format.Common;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => new ReadableMetadata(new[] {
            new ReadableMetadataHeader("Joint Object Coding", new[] {
                new ReadableMetadataField("joc_num_channels", "Number of channels in the JOC downmix", ChannelCount),
                new ReadableMetadataField("joc_num_objects", "Number of rendered dynamic objects", ObjectCount),
                new ReadableMetadataField("joc_clipgain", "Multiplier for the output signal's amplitude", Gain)
            })
        });
    }
}