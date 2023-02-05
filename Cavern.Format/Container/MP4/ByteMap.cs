using System.Drawing;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// Contains which sample of the input starts from which file offset and how many bytes should be read.
    /// </summary>
    internal class ByteMap {
        /// <summary>
        /// Locations in the source file for all samples.
        /// </summary>
        public readonly (ulong offset, uint length)[] map;

        /// <summary>
        /// Decodes which sample of the input starts from which file offset and how many bytes should be read.
        /// </summary>
        public ByteMap(NestedBox sampleTableBox) {
            ChunkOffsetBox chunkOffsets = sampleTableBox[chunkOffset32] as ChunkOffsetBox;
            chunkOffsets ??= sampleTableBox[chunkOffset64] as ChunkOffsetBox;
            SampleSizeBox sampleSizes = sampleTableBox[sampleSizeBox] as SampleSizeBox;
            SampleToChunkBox samplesPerChunk = sampleTableBox[sampleToChunkBox] as SampleToChunkBox;

            map = new (ulong, uint)[sampleSizes.size == 0 ? sampleSizes.sizes.LongLength :
                samplesPerChunk.GetSampleCount(chunkOffsets.offsets.LongLength)];
            (uint firstChunk, uint samplesPerChunk, uint formatIndex)[] locations = samplesPerChunk.locations;
            long cluster = 0; // Which entry of locations we're in
            uint chunk = locations[0].firstChunk;
            ulong offset = chunkOffsets.offsets[0];
            ulong samplesPerChunkL = locations[0].samplesPerChunk;
            long chunksUntilNextCluster = locations.Length == 1 ? chunkOffsets.offsets.LongLength - 1 :
                (locations[1].firstChunk - locations[0].firstChunk);

            for (int i = 0; i < map.Length;) { // Get the map chunk by chunk
                if (sampleSizes.size == 0) {
                    for (ulong sample = 0; sample < samplesPerChunkL; sample++) {
                        map[i] = (offset, sampleSizes.sizes[i]);
                        offset += sampleSizes.sizes[i++];
                    }
                } else {
                    for (ulong sample = 0; sample < samplesPerChunkL; sample++) {
                        map[i++] = (offset, sampleSizes.size);
                        offset += sampleSizes.size;
                    }
                }

                offset = chunkOffsets.offsets[++chunk];
                if (--chunksUntilNextCluster == 0) {
                    chunksUntilNextCluster = ++cluster == locations.Length ? chunkOffsets.offsets.LongLength - locations[^1].firstChunk :
                        (locations[cluster].firstChunk - locations[cluster - 1].firstChunk);
                    samplesPerChunkL = locations[cluster - 1].samplesPerChunk;
                }
            }
        }
    }
}