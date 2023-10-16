using System;
using System.Collections.Generic;
using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes an object audio element metadata block.
    /// </summary>
    partial class OAElementMD {
        /// <summary>
        /// Gets the timecode of the first update in this block.
        /// </summary>
        public short MinOffset => blockOffsetFactor[0];

        /// <summary>
        /// The movement data of the next block was already set to motion.
        /// </summary>
        bool[] blockUsed = new bool[0];

        /// <summary>
        /// The object at index had a valid position in the last frame.
        /// </summary>
        bool[] updateLast;

        /// <summary>
        /// The object at index has a valid position in the current frame.
        /// </summary>
        bool[] updateNow;

        /// <summary>
        /// Global sample offset, applied to all info blocks.
        /// </summary>
        byte sampleOffset;

        /// <summary>
        /// The beginning of each info block in samples.
        /// </summary>
        /// <remarks>Negative numbers mean that this element doesn't contain object location data.</remarks>
        short[] blockOffsetFactor;

        /// <summary>
        /// Time to fade to a new position in samples for each info block.
        /// </summary>
        short[] rampDuration;

        /// <summary>
        /// Rendering info for each object's updates. The first dimension is the object, the second is the info block.
        /// </summary>
        /// <remarks>Can be null if the element is not an object element.</remarks>
        ObjectInfoBlock[][] infoBlocks;

        /// <summary>
        /// The position of each object to move to.
        /// </summary>
        Vector3[] future;

        /// <summary>
        /// Samples until reaching the <see cref="future"/>.
        /// </summary>
        int futureDistance;

        /// <summary>
        /// Decodes an object audio element metadata block.
        /// </summary>
        public void Read(BitExtractor extractor, bool alternateObjectPresent, int objectCount, int bedOrISFObjects) {
            int elementIndex = extractor.Read(4);
            int endPos = extractor.Position + ExtensibleMetadataExtensions.VariableBits(extractor, 4, 4) + 1;
            extractor.Skip(alternateObjectPresent ? 5 : 1);
            if (elementIndex == objectElementIndex) {
                ObjectElement(extractor, objectCount, bedOrISFObjects);
            } else { // Other elements are unused by encoders
                blockOffsetFactor = new[] { (short)(-1 - elementIndex) };
            }
            extractor.Position = endPos; // Padding
        }

        /// <summary>
        /// Set the object properties from metadata.
        /// </summary>
        /// <param name="timecode">Samples since the beginning of the audio frame</param>
        /// <param name="sources">The sources used for rendering this track</param>
        public void UpdateSources(int timecode, IReadOnlyList<Source> sources) {
            Array.Copy(updateNow, updateLast, updateNow.Length);

            for (int blk = 0; blk < blockUsed.Length; ++blk) {
                if (blockUsed[blk]) {
                    continue;
                }

                if (timecode > blockOffsetFactor[blk]) {
                    blockUsed[blk] = true;
                    futureDistance = rampDuration[blk] - (timecode - blockOffsetFactor[blk]);
                    for (int obj = 0; obj < infoBlocks.Length; ++obj) {
                        ObjectInfoBlock[] block = infoBlocks[obj];
                        updateNow[obj] = block[blk].ValidPosition;
                        future[obj] = block[blk].UpdateSource(sources[obj]);
                    }
                }
            }

            if (futureDistance > 0) {
                float t = Math.Min(QuadratureMirrorFilterBank.subbands / (float)futureDistance, 1);
                for (int obj = 0; obj < infoBlocks.Length; ++obj) {
                    if (updateNow[obj]) {
                        sources[obj].Position = updateLast[obj] ?
                            Vector3.Lerp(sources[obj].Position, future[obj], t) : future[obj];
                    }
                }
                futureDistance -= QuadratureMirrorFilterBank.subbands;
            }
        }

        void ObjectElement(BitExtractor extractor, int objectCount, int bedOrISFObjects) {
            MDUpdateInfo(extractor);
            if (!extractor.ReadBit()) { // Reserved
                extractor.Skip(5);
            }

            if (blockUsed.Length != rampDuration.Length || infoBlocks.Length != objectCount) {
                blockUsed = new bool[rampDuration.Length];
                updateLast = new bool[objectCount];
                updateNow = new bool[objectCount];
                infoBlocks = new ObjectInfoBlock[objectCount][];
                for (int obj = 0; obj < objectCount; ++obj) {
                    infoBlocks[obj] = new ObjectInfoBlock[rampDuration.Length];
                    for (int blk = 0; blk < infoBlocks[obj].Length; ++blk) {
                        infoBlocks[obj][blk] = new ObjectInfoBlock();
                    }
                }
                future = new Vector3[objectCount];
            }

            blockUsed.Clear();
            for (int obj = 0; obj < objectCount; ++obj) {
                for (int blk = 0; blk < infoBlocks[obj].Length; ++blk) {
                    infoBlocks[obj][blk].Update(extractor, blk, obj < bedOrISFObjects);
                }
            }
        }

        void MDUpdateInfo(BitExtractor extractor) {
            sampleOffset = extractor.Read(2) switch {
                0 => 0,
                1 => sampleOffsetIndex[extractor.Read(2)],
                2 => (byte)extractor.Read(5),
                _ => throw new UnsupportedFeatureException("mdOffset"),
            };
            blockOffsetFactor = new short[extractor.Read(3) + 1];
            rampDuration = new short[blockOffsetFactor.Length];
            for (int blk = 0; blk < rampDuration.Length; ++blk) {
                BlockUpdateInfo(extractor, blk);
            }
        }

        void BlockUpdateInfo(BitExtractor extractor, int blk) {
            blockOffsetFactor[blk] = (short)(extractor.Read(6) + sampleOffset);
            int rampDurationCode = extractor.Read(2);
            if (rampDurationCode == 3) {
                if (extractor.ReadBit()) {
                    rampDuration[blk] = rampDurationIndex[extractor.Read(4)];
                } else {
                    rampDuration[blk] = (short)extractor.Read(11);
                }
            } else {
                rampDuration[blk] = rampDurations[rampDurationCode];
            }
        }

        /// <summary>
        /// Marks an object positioning frame.
        /// </summary>
        const int objectElementIndex = 1;

        static readonly byte[] sampleOffsetIndex = { 8, 16, 18, 24 };
        static readonly short[] rampDurations = { 0, 512, 1536 };
        static readonly short[] rampDurationIndex =
            { 32, 64, 128, 256, 320, 480, 1000, 1001, 1024, 1600, 1601, 1602, 1920, 2000, 2002, 2048 };
    }
}