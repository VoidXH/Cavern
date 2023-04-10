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
        Codec ParseCodec(uint formatId) => trackCodecs.ContainsKey(formatId) ? trackCodecs[formatId] : Codec.Unknown;
    }
}