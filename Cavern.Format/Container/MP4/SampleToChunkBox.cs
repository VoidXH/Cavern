using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Contains which samples are located in which chunk (32 bit version).
    /// </summary>
    internal class SampleToChunkBox : Box {
        /// <summary>
        /// From which chunk, how many samples are contained in a chunk, and in which format of the <see cref="SampleDescriptionBox"/>.
        /// </summary>
        (uint firstChunk, uint samplesPerChunk, uint formatIndex)[] Locations { get; }

        /// <summary>
        /// Contains which samples are located in which chunk (32 bit version).
        /// </summary>
        public SampleToChunkBox(uint length, Stream reader) : base(length, sampleToChunkBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            Locations = new(uint, uint, uint)[reader.ReadUInt32BE()];
            for (int i = 0; i < Locations.Length; i++) {
                Locations[i] = (reader.ReadUInt32BE(), reader.ReadUInt32BE(), reader.ReadUInt32BE());
            }
        }
    }
}