using System.IO;

using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Contains which samples are located in which chunk.
    /// </summary>
    internal class SampleToChunkBox : Box {
        /// <summary>
        /// From which chunk, how many samples are contained in a chunk, and in which format of the <see cref="SampleDescriptionBox"/>.
        /// </summary>
        public readonly (uint firstChunk, uint samplesPerChunk, uint formatIndex)[] locations;

        /// <summary>
        /// Contains which samples are located in which chunk.
        /// </summary>
        public SampleToChunkBox(uint length, Stream reader) : base(length, sampleToChunkBox, reader) {
            reader.Position += 4; // Version byte and zero flags
            locations = new(uint, uint, uint)[reader.ReadUInt32BE()];
            for (int i = 0; i < locations.Length; i++) {
                locations[i] = (reader.ReadUInt32BE(), reader.ReadUInt32BE(), reader.ReadUInt32BE());
            }
        }

        /// <summary>
        /// When each sample is the same size and there's no sample count in the <see cref="SampleSizeBox"/>, the number of samples
        /// have to be calculated from this box.
        /// </summary>
        public long GetSampleCount(long totalChunks) {
            long result = 0;
            for (int i = 1; i < locations.Length; i++) {
                result += (locations[i].firstChunk - locations[i - 1].firstChunk) * locations[i - 1].samplesPerChunk;
            }
            return result + (totalChunks - locations[^1].firstChunk) * locations[^1].samplesPerChunk;
        }
    }
}