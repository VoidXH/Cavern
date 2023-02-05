using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Box of offsets for all chunks.
    /// </summary>
    internal class ChunkOffsetBox : Box {
        /// <summary>
        /// Start offset in the file for each chunk.
        /// </summary>
        public readonly ulong[] offsets;

        /// <summary>
        /// Box of offsets for all chunks.
        /// </summary>
        public ChunkOffsetBox(uint length, uint header, Stream reader) : base(length, header, reader) {
            reader.Position += 4; // Version byte and zero flags
            offsets = new ulong[reader.ReadUInt32BE()];
            if (header == chunkOffset32) {
                for (int i = 0; i < offsets.Length; i++) {
                    offsets[i] = reader.ReadUInt32BE();
                }
            } else {
                for (int i = 0; i < offsets.Length; i++) {
                    offsets[i] = reader.ReadUInt64BE();
                }
            }
        }
    }
}