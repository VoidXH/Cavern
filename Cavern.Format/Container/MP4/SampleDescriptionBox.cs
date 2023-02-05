using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Metadata box containing codec information.
    /// </summary>
    internal class SampleDescriptionBox : Box {
        /// <summary>
        /// Used codecs for referenced raw data with their extra values. References are contained in reference boxes.
        /// </summary>
        public readonly (Codec codec, ushort dataReferenceIndex, byte[] extra)[] formats;

        /// <summary>
        /// Metadata box containing codec information.
        /// </summary>
        public SampleDescriptionBox(uint length, Stream reader) : base(length, sampleDescriptionBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            formats = new (Codec, ushort, byte[])[reader.ReadUInt32BE()];
            for (uint i = 0; i < formats.Length; i++) {
                int size = reader.ReadInt32BE();
                Codec codec = ParseCodec(reader.ReadUInt32BE());
                reader.Position += 6; // Reserved
                formats[i] = (codec, reader.ReadUInt16BE(), reader.ReadBytes(size - 16));
            }
        }

        /// <summary>
        /// Parse the format-specific values into the <see cref="Codec"/> enumeration.
        /// </summary>
        Codec ParseCodec(uint formatId) => formatId switch {
            0x64766831 => Codec.HEVC_DolbyVision, // dvh1
            0x64766865 => Codec.HEVC_DolbyVision, // dvhe
            0x68657631 => Codec.HEVC, // hev1
            0x68766331 => Codec.HEVC, // hvc1
            0x61766331 => Codec.AVC, // avc1
            0x65632D33 => Codec.EnhancedAC3, // ec-3
            0x666C3332 => Codec.PCM_Float, // fl32
            0x696E3234 => Codec.PCM_LE, // in24
            0x6970636D => Codec.PCM_LE, // ipcm
            0x6C70636D => Codec.PCM_LE, // lpcm
            0x736F7774 => Codec.PCM_LE, // sowt
            0x6D6C7061 => Codec.TrueHD, // mlpa
            0x64747368 => Codec.DTS_HD, // dtsh
            0x6474736C => Codec.DTS_HD, // dtsl
            0x664C6143 => Codec.FLAC, // fLaC
            0x4F707573 => Codec.Opus, // Opus
            0x64747363 => Codec.DTS, // dtsc
            0x61632D33 => Codec.AC3, // ac-3
            _ => Codec.Unknown
        };
    }
}