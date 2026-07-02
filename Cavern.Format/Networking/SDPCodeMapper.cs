using System;
using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Exceptions;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Maps between <see cref="Codec"/> and its SDP name.
    /// </summary>
    public static class SDPCodeMapper {
        /// <summary>
        /// Converts codec to SDP name. Bit depth is only used for PCM codecs.
        /// </summary>
        public static string GetSDPName(Codec codec, BitDepth bitDepth) {
            for (int i = 0; i < supported.Count; i++) {
                if (supported[i].Codec == codec) {
                    return string.Format(supported[i].Name, (int)bitDepth);
                }
            }
            throw new UnsupportedCodecException(codec);
        }

        /// <summary>
        /// Converts SDP name to codec.
        /// </summary>
        public static Codec GetCodec(string sdpName) {
            if (TryGetPcmBitDepth(sdpName, out _)) {
                return Codec.PCM_LE;
            }

            for (int i = 0; i < supported.Count; i++) {
                if (string.Equals(supported[i].Name, sdpName, StringComparison.OrdinalIgnoreCase)) {
                    return supported[i].Codec;
                }
            }
            throw new UnsupportedCodecException(sdpName);
        }

        /// <summary>
        /// Extracts bit depth from SDP name. Returns false if not a PCM-style SDP name.
        /// </summary>
        public static bool TryGetPcmBitDepth(string sdpName, out int bitDepth) {
            bitDepth = 0;
            return !string.IsNullOrEmpty(sdpName) && sdpName.Length > 1 && (sdpName[0] == 'L' || sdpName[0] == 'l') && int.TryParse(sdpName.AsSpan(1), out bitDepth);
        }

        /// <summary>
        /// All supported codecs and their SDP names. The bit depth is formatted into {0}.
        /// </summary>
        static readonly List<(Codec Codec, string Name)> supported = new List<(Codec Codec, string Name)> {
            (Codec.HEVC, "H265"),
            (Codec.AVC, "H264"),
            (Codec.ADM_BWF, "BW64"),
            (Codec.TrueHD, "MLP"),
            (Codec.EnhancedAC3, "EAC3"),
            (Codec.PCM_LE, "L{0}"),
            (Codec.DTS_HD, "DTSHD"),
            (Codec.FLAC, "FLAC"),
            (Codec.Opus, "opus"),
            (Codec.DTS, "DTS"),
            (Codec.AC3, "AC3"),
        };
    }
}
