using System;
using System.Collections.Generic;
using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes an object audio element metadata block.
    /// </summary>
    class OAElementMD {
        /// <summary>
        /// Gets the timecode of the first update in this block.
        /// </summary>
        public int MinOffset => blockOffsetFactor[0];

        /// <summary>
        /// Last decoded precise object update positions.
        /// </summary>
        static Vector3[] lastPrecisePositions;

        /// <summary>
        /// Global sample offset, applied to all info blocks.
        /// </summary>
        int sampleOffset;

        /// <summary>
        /// The beginning of each info block in samples.
        /// </summary>
        int[] blockOffsetFactor;

        /// <summary>
        /// Time to fade to a new position in samples for each info block.
        /// </summary>
        int[] rampDuration;

        /// <summary>
        /// Rendering info for each object's updates. The first dimension is the object, the second is the info block.
        /// </summary>
        /// <remarks>Can be null if the element is not an object element.</remarks>
        ObjectInfoBlock[][] infoBlocks;

        /// <summary>
        /// Decodes an object audio element metadata block.
        /// </summary>
        public OAElementMD(BitExtractor extractor, bool alternateObjectPresent, int objectCount, int bedOrISFObjects) {
            if (lastPrecisePositions == null)
                lastPrecisePositions = new Vector3[objectCount];
            int elementIndex = extractor.Read(4);
            int endPos = extractor.Position + VariableBitsMax(extractor, 4, 4) + 1;
            extractor.Skip(alternateObjectPresent ? 5 : 1);
            if (elementIndex == 1)
                ObjectElement(extractor, objectCount, bedOrISFObjects);
            // TODO: support other element types
            extractor.Position = endPos; // Padding
        }

        /// <summary>
        /// Set the object properties from metadata.
        /// </summary>
        /// <param name="timecode">Samples since the beginning of the audio frame</param>
        /// <param name="sources">The sources used for rendering this track</param>
        /// <param name="lastHoldPos">A helper array for each object, holding the last non-ramped position</param>
        public void SetPositions(int timecode, IReadOnlyList<Source> sources, Vector3[] lastHoldPos) {
            for (int obj = 0; obj < infoBlocks.Length; ++obj) {
                for (int blk = 0; blk < infoBlocks[obj].Length; ++blk) {
                    if (timecode > blockOffsetFactor[blk] + rampDuration[blk]) {
                        if (blk == 0)
                            sources[obj].Position = lastHoldPos[obj];
                        break;
                    }
                    infoBlocks[obj][blk].UpdateSource(sources[obj], ref lastPrecisePositions[obj]);
                    if (timecode > blockOffsetFactor[blk]) {
                        float t = (timecode - blockOffsetFactor[blk]) / (float)rampDuration[blk];
                        sources[obj].Position = Vector3.Lerp(lastHoldPos[obj], sources[obj].Position, Math.Min(t, 1));
                        lastHoldPos[obj] = sources[obj].Position;
                        break;
                    }
                    lastHoldPos[obj] = sources[obj].Position;
                }
            }
        }

        void ObjectElement(BitExtractor extractor, int objectCount, int bedOrISFObjects) {
            MDUpdateInfo(extractor);
            if (!extractor.ReadBit()) // Reserved
                extractor.Skip(5);

            infoBlocks = new ObjectInfoBlock[objectCount][];
            for (int obj = 0; obj < objectCount; ++obj) {
                infoBlocks[obj] = new ObjectInfoBlock[rampDuration.Length];
                for (int blk = 0; blk < infoBlocks[obj].Length; ++blk)
                    infoBlocks[obj][blk] = new ObjectInfoBlock(extractor, blk, obj < bedOrISFObjects);
            }
        }

        void MDUpdateInfo(BitExtractor extractor) {
            sampleOffset = extractor.Read(2) switch {
                0 => 0,
                1 => sampleOffsetIndex[extractor.Read(2)],
                2 => extractor.Read(5),
                _ => throw new UnsupportedFeatureException("mdOffset"),
            };
            blockOffsetFactor = new int[extractor.Read(3) + 1];
            rampDuration = new int[blockOffsetFactor.Length];
            for (int blk = 0; blk < rampDuration.Length; ++blk)
                BlockUpdateInfo(extractor, blk);
        }

        void BlockUpdateInfo(BitExtractor extractor, int blk) {
            blockOffsetFactor[blk] = extractor.Read(6) + sampleOffset;
            int rampDurationCode = extractor.Read(2);
            if (rampDurationCode == 3) {
                if (extractor.ReadBit())
                    rampDuration[blk] = rampDurationIndex[extractor.Read(4)];
                else
                    rampDuration[blk] = extractor.Read(11);
            } else
                rampDuration[blk] = rampDurations[rampDurationCode];
        }

        static readonly int[] sampleOffsetIndex = { 8, 16, 18, 24 };
        static readonly int[] rampDurations = { 0, 512, 1536 };
        static readonly int[] rampDurationIndex =
            { 32, 64, 128, 256, 320, 480, 1000, 1001, 1024, 1600, 1601, 1602, 1920, 2000, 2002, 2048 };

        int VariableBitsMax(BitExtractor extractor, int n, int groups) {
            int value = 0;
            int group = 1;
            int read = extractor.Read(n);
            value += read;
            bool readMore = extractor.ReadBit();
            if (groups > group) {
                if (readMore) {
                    value <<= n;
                    value += (1 << n);
                }
                while (readMore) {
                    read = extractor.Read(n);
                    value += read;
                    readMore = extractor.ReadBit();
                    if (group >= groups)
                        break;
                    if (readMore) {
                        value <<= n;
                        value += (1 << n);
                        group += 1;
                    }
                }
            }
            return value;
        }
    }
}