using Cavern.Format.Common;
using Cavern.Format.Utilities;

using static Cavern.Format.Consts.MP4Consts;

namespace Cavern.Format.Container.MP4 {
    /// <summary>
    /// An MP4 file's <see cref="Track"/> with stream positioning information.
    /// </summary>
    public class MP4Track : Track {
        /// <summary>
        /// Contains which sample of the input starts from which file offset and how many bytes should be read.
        /// </summary>
        internal readonly (ulong offset, uint length)[] map;

        /// <summary>
        /// The next entry to decode from the <see cref="map"/>.
        /// </summary>
        internal long nextSample;

        /// <summary>
        /// How many units make up a second.
        /// </summary>
        readonly uint timeScale;

        /// <summary>
        /// Box used for getting seek positions.
        /// </summary>
        readonly TimeToSampleBox seekerBox;

        /// <summary>
        /// An MP4 file's <see cref="Track"/> with stream positioning information. The format and its extra information is parsed here.
        /// </summary>
        internal MP4Track(NestedBox sampleTableBox, uint timeScale) {
            if (sampleTableBox == null) {
                throw new MissingElementException(Consts.MP4Consts.sampleTableBox.ToFourCC());
            }
            if (sampleTableBox[sampleDescriptionBox] is SampleDescriptionBox stsd && stsd.formats.Length == 1) {
                Format = stsd.formats[0].codec;
                if (Format.IsAudio()) {
                    byte[] extra = stsd.formats[0].extra;
                    Extra = new TrackExtraAudio {
                        Bits = (BitDepth)extra.ReadInt16(11),
                        ChannelCount = extra.ReadInt16(9),
                        SampleRate = timeScale
                    };
                }
            }

            this.timeScale = timeScale;
            seekerBox = sampleTableBox[timeToSampleBox] as TimeToSampleBox;

            ChunkOffsetBox chunkOffsets = sampleTableBox[chunkOffset32] as ChunkOffsetBox;
            chunkOffsets ??= sampleTableBox[chunkOffset64] as ChunkOffsetBox;
            SampleSizeBox sampleSizes = sampleTableBox[sampleSizeBox] as SampleSizeBox;
            SampleToChunkBox samplesPerChunk = sampleTableBox[sampleToChunkBox] as SampleToChunkBox;

            map = new (ulong, uint)[sampleSizes.size == 0 ? sampleSizes.sizes.LongLength :
                samplesPerChunk.GetSampleCount(chunkOffsets.offsets.LongLength)];
            (uint firstChunk, uint samplesPerChunk, uint formatIndex)[] locations = samplesPerChunk.locations;
            long cluster = 0; // Which entry of locations we're in
            uint chunk = locations[0].firstChunk - 1;
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

                if (++chunk == chunkOffsets.offsets.Length) {
                    break; // It's actually a corrupt file, but might work with slow seeking
                }
                offset = chunkOffsets.offsets[chunk];
                if (--chunksUntilNextCluster == 0) {
                    if (i == map.Length || locations.Length == 1) {
                        break;
                    }
                    if (++cluster == locations.Length - 1) {
                        chunksUntilNextCluster = chunkOffsets.offsets.LongLength - locations[^1].firstChunk;
                        samplesPerChunkL = (ulong)(map.LongLength - i);
                    } else {
                        chunksUntilNextCluster = locations[cluster + 1].firstChunk - locations[cluster].firstChunk;
                        samplesPerChunkL = locations[cluster].samplesPerChunk;
                    }
                }
            }
        }

        /// <summary>
        /// Get which is the current sample at a given time in seconds, and its corrected position.
        /// </summary>
        public (long index, double time) GetSample(double time) {
            long targetTime = (long)(time * timeScale);
            long currentTime = 0;
            long currentSample = 0;
            (uint sampleCount, uint duration)[] timings = seekerBox.durations;
            for (int i = 0; i < timings.Length; i++) {
                long block = timings[i].sampleCount * timings[i].duration;
                if (currentTime + block < targetTime) {
                    currentTime += block;
                    currentSample += timings[i].sampleCount;
                    continue;
                }

                long duration = timings[i].duration;
                for (int j = 0; j < timings[i].sampleCount; j++) {
                    if (currentTime + duration < targetTime) {
                        currentTime += duration;
                        ++currentSample;
                        continue;
                    }
                    return (currentSample, currentTime / (double)timeScale);
                }
            }
            return (-1, 0);
        }
    }
}