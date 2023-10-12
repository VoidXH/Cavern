using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header : IMetadataSupplier {
        /// <summary>
        /// Gets the metadata for this codec in a human-readable format.
        /// </summary>
        public ReadableMetadata GetMetadata() => new ReadableMetadata(new[] {
            new ReadableMetadataHeader("Bitstream information", new[] {
                new ReadableMetadataField("strmtyp", "Stream type ID", (int)StreamType),
                new ReadableMetadataField("strmtyp", "Stream type name", StreamType),
                new ReadableMetadataField("substreamid", "Substream ID", SubstreamID),
                new ReadableMetadataField("frmsiz", "Frame size (in 16-bit words)", WordsPerSyncframe),
                new ReadableMetadataField("frmsiz (in b/f)", $"Bytes per {Blocks * 256} samples", (WordsPerSyncframe << 1) + " B"),
                new ReadableMetadataField("frmsiz (in kb/s)", $"Track bitrate",
                    (WordsPerSyncframe << 5) * SampleRate / (Blocks * 256 * 1024) + " kbps"),
                new ReadableMetadataField("fscod", "Sample rate code", SampleRateCode),
                new ReadableMetadataField("fscod (decoded)", "Sample rate", SampleRate + " Hz"),
                new ReadableMetadataField("numblks", "Number of audio blocks per frace", Blocks),
                new ReadableMetadataField("acmod", "Audio channel mode", ChannelMode),
                new ReadableMetadataField("lfeon", "Low Frequency Effects channel exists", LFE),
                new ReadableMetadataField("chanmap", "Custom channel map", channelMapping),
                new ReadableMetadataField("chanmap (decoded)", "Custom channel map, or default channels (from acmod) if not present",
                    string.Join(", ", ChannelPrototype.GetShortNames(GetChannelArrangement()))),
                new ReadableMetadataField("mixmdate", "Mixing metadata exists", mixingEnabled),
                new ReadableMetadataField("infomdate", "Informational metadata exists", informationalMetadataEnabled),
                new ReadableMetadataField("blkid", "Block ID", blkid),
                new ReadableMetadataField("bsid", "Bitstream format ID", (int)Decoder),
                new ReadableMetadataField("bsid (decoded)", "Bitstream format name", Decoder),
                new ReadableMetadataField("bsmod", "Bitstream mode ID", bsmod),
                new ReadableMetadataField("cmixlev", "Center downmix code", centerDownmix),
                new ReadableMetadataField("cmixlev (LtRt)", "Center downmix level in Lt/Rt mode",
                    QMath.GainToDb(GetCenterDownmixLtRt()).ToString("0.0 dB")),
                new ReadableMetadataField("cmixlev (LoRo)", "Center downmix level in Lo/Ro mode",
                    QMath.GainToDb(GetCenterDownmixLoRo()).ToString("0.0 dB")),
                new ReadableMetadataField("surmixlev", "Surround downmix code", surroundDownmix),
                new ReadableMetadataField("dsurmod", "Stereo encoding technology recommendation ID", dsurmod),
                new ReadableMetadataField("dialnorm", "Dialogue normalization", dialnorm),
                new ReadableMetadataField("compre", "Compression exists", compr.HasValue),
                new ReadableMetadataField("compr", "Compression gain word", compr),
                new ReadableMetadataField("langcode", "Language code exists", langcod.HasValue),
                new ReadableMetadataField("langcod", "Language code", langcod),
                new ReadableMetadataField("audprodie", "Audio production information exists", audprodie),
                new ReadableMetadataField("mixlevel", "Mixing level code", mixlevel),
                new ReadableMetadataField("mixlevel (decoded)", "Mixing level", 80 + mixlevel + " dB"),
                new ReadableMetadataField("roomtyp", "Room type ID", roomtyp),
                new ReadableMetadataField("dialnorm2", "Dialogue normalization, channel 2", dialnorm2),
                new ReadableMetadataField("compr2e", "Compression exists, channel 2", compr2.HasValue),
                new ReadableMetadataField("compr2", "Compression gain word, channel 2", compr2),
                new ReadableMetadataField("langcod2e", "Language code exists, channel 2", langcod2.HasValue),
                new ReadableMetadataField("langcod2", "Language code, channel 2", langcod2),
                new ReadableMetadataField("audprodi2e", "Audio production information exists, channel2", audprodi2e),
                new ReadableMetadataField("roomtyp2", "Room type ID, channel 2", roomtyp2),
                new ReadableMetadataField("copyrightb", "Copyright bit", copyrightBit),
                new ReadableMetadataField("origbs", "Original bitstream", originalBitstream),
                new ReadableMetadataField("convsync", "Converter synchronization flag", convsync),
                new ReadableMetadataField("addbsil", "Additional bitstream information size", addbsilen + " B"),
            })
        });
    }
}