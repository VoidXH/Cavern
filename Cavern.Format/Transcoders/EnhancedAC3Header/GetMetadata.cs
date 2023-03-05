using System.Collections.Generic;

using Cavern.Format.Common;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => new ReadableMetadata(new (string name, IReadOnlyList<ReadableMetadataField> fields)[] {
            ("Bitstream information", new ReadableMetadataField[] {
                new ReadableMetadataField("strmtyp", "Stream type", StreamType.ToString()),
                new ReadableMetadataField("substreamid", "Substream ID", SubstreamID),
                new ReadableMetadataField("frmsiz", "Frame size (in 16-bit words)", WordsPerSyncframe),
                new ReadableMetadataField("fscod", "Sample rate code", SampleRateCode),
                new ReadableMetadataField("numblks", "Number of audio blocks", Blocks),
                new ReadableMetadataField("acmod", "Audio channel mode", ChannelMode),
                new ReadableMetadataField("lfeon", "Low Frequency Effects channel enabled", LFE),
                new ReadableMetadataField("bsid", "Bitstream format ID", Decoder.ToString()),
                new ReadableMetadataField("dialnorm", "Dialogue normalization", dialnorm),
                new ReadableMetadataField("compr", "Compression gain word", compr),
                new ReadableMetadataField("dialnorm2", "Dialogue normalization, channel 2", dialnorm2),
                new ReadableMetadataField("compr2", "Compression gain word, channel 2", compr2),
                new ReadableMetadataField("chanmap", "Custom channel map", channelMapping),
                new ReadableMetadataField("chanmap (decoded)", "Custom or default channel map", GetChannelArrangement()),
                new ReadableMetadataField("mixmdate", "Mixing metadata exists", mixingEnabled),
                new ReadableMetadataField("infomdate", "Informational metadata exists", informationalMetadataEnabled),
                new ReadableMetadataField("convsync", "Converter synchronization flag", convsync),
                new ReadableMetadataField("blkid", "Block ID", blkid),
                new ReadableMetadataField("addbsil", "Additional bit stream information length (in bytes)", addbsilen),
            })
        });
    }
}