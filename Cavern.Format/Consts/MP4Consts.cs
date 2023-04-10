using Cavern.Format.Common;
using System.Collections.Generic;

namespace Cavern.Format.Consts {
    /// <summary>
    /// Constants used for the MP4 file format.
    /// </summary>
    internal static class MP4Consts {
        /// <summary>
        /// FourCC marker of the file type box (ftyp).
        /// </summary>
        public const uint fileTypeBox = 0x66747970;

        /// <summary>
        /// FourCC marker of the boxes to skip (free).
        /// </summary>
        public const uint freeBox = 0x66726565;

        /// <summary>
        /// FourCC marker of the metadata box (moov).
        /// </summary>
        public const uint metadataBox = 0x6D6F6F76;

        /// <summary>
        /// FourCC marker of the <see cref="metadataBox"/>'s header box (mvhd).
        /// </summary>
        public const uint metadataHeaderBox = 0x6D766864;

        /// <summary>
        /// FourCC marker of the box where raw bytes of the tracks can be found (mdat).
        /// </summary>
        public const uint rawBox = 0x6D646174;

        /// <summary>
        /// FourCC marker of the track metadata box (trak).
        /// </summary>
        public const uint trackBox = 0x7472616B;

        /// <summary>
        /// FourCC marker of the <see cref="trackBox"/>'s header child box (tkhd).
        /// </summary>
        public const uint trackHeaderBox = 0x746B6864;

        /// <summary>
        /// FourCC marker of the <see cref="trackBox"/>'s media child box (mdia).
        /// </summary>
        public const uint mediaBox = 0x6D646961;

        /// <summary>
        /// FourCC marker of the <see cref="mediaBox"/>'s media header box (mdhd).
        /// </summary>
        public const uint mediaHeaderBox = 0x6D646864;

        /// <summary>
        /// FourCC marker of the <see cref="mediaBox"/>'s media information box (minf).
        /// </summary>
        public const uint mediaInfoBox = 0x6D696E66;

        /// <summary>
        /// FourCC marker of the <see cref="mediaInfoBox"/>'s sample table box (stbl).
        /// </summary>
        public const uint sampleTableBox = 0x7374626C;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s sample description box (stsd).
        /// </summary>
        public const uint sampleDescriptionBox = 0x73747364;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s sample durations for media samples (stts).
        /// </summary>
        public const uint timeToSampleBox = 0x73747473;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s chunk assignment (stsc).
        /// </summary>
        public const uint sampleToChunkBox = 0x73747363;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s sample size list (stsz).
        /// </summary>
        public const uint sampleSizeBox = 0x7374737A;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s 32-bit chunk size list (stco).
        /// </summary>
        public const uint chunkOffset32 = 0x7374636F;

        /// <summary>
        /// FourCC marker of the <see cref="sampleTableBox"/>'s 64-bit chunk size list (co64).
        /// </summary>
        public const uint chunkOffset64 = 0x636F3634;

        /// <summary>
        /// The FourCC markers of <see cref="Codec"/>s for <see cref="sampleDescriptionBox"/>es.
        /// </summary>
        public static readonly Dictionary<uint, Codec> trackCodecs = new Dictionary<uint, Codec> {
            [0x64766831] = Codec.HEVC_DolbyVision, // dvh1
            [0x64766865] = Codec.HEVC_DolbyVision, // dvhe
            [0x68657631] = Codec.HEVC, // hev1
            [0x68766331] = Codec.HEVC, // hvc1
            [0x61766331] = Codec.AVC, // avc1
            [0x65632D33] = Codec.EnhancedAC3, // ec-3
            [0x666C3332] = Codec.PCM_Float, // fl32
            [0x696E3234] = Codec.PCM_LE, // in24
            [0x6970636D] = Codec.PCM_LE, // ipcm
            [0x6C70636D] = Codec.PCM_LE, // lpcm
            [0x736F7774] = Codec.PCM_LE, // sowt
            [0x6D6C7061] = Codec.TrueHD, // mlpa
            [0x64747368] = Codec.DTS_HD, // dtsh
            [0x6474736C] = Codec.DTS_HD, // dtsl
            [0x664C6143] = Codec.FLAC, // fLaC
            [0x4F707573] = Codec.Opus, // Opus
            [0x64747363] = Codec.DTS, // dtsc
            [0x61632D33] = Codec.AC3, // ac-3
        };
    }
}