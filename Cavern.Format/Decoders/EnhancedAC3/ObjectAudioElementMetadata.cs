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
        /// Rendering info for each object's updates. The first dimension is the object, the second is the info block.
        /// </summary>
        /// <remarks>Can be null if the element is not an object element.</remarks>
        ObjectInfoBlock[][] infoBlocks;

        /// <summary>
        /// Last decoded precise object update positions.
        /// </summary>
        static Vector3[] lastPrecisePositions;

        /// <summary>
        /// Decodes an object audio element metadata block.
        /// </summary>
        public OAElementMD(BitExtractor extractor, bool b_alternate_object_data_present, int objectCount,
            int bed_or_isf_objects) {
            if (lastPrecisePositions == null || lastPrecisePositions.Length != objectCount)
                lastPrecisePositions = new Vector3[objectCount];

            oa_element_id_idx = extractor.Read(4);
            oa_element_size = VariableBitsMax(extractor, 4, 4) + 1;
            int endPos = extractor.Position + oa_element_size;
            if (b_alternate_object_data_present)
                alternate_object_data_id_idx = extractor.Read(4);
            b_discard_unknown_element = extractor.ReadBit();
            if (oa_element_id_idx == 1)
                ObjectElement(extractor, objectCount, bed_or_isf_objects);
            // TODO: support other element types
            extractor.Position = endPos; // Padding
        }

        /// <summary>
        /// Get where each object is located in the room.
        /// </summary>
        public Vector3[] GetPositions() {
            Vector3[] result = new Vector3[infoBlocks.Length];
            for (int pos = 0; pos < infoBlocks.Length; ++pos)
                for (int blk = 0; blk < infoBlocks[pos].Length; ++blk)
                    infoBlocks[pos][blk].UpdatePosition(ref result[pos], ref lastPrecisePositions[pos]);
            return result;
        }

        void ObjectElement(BitExtractor extractor, int objectCount, int bed_or_isf_objects) {
            MDUpdateInfo(extractor);
            bool b_reserved_data_not_present = extractor.ReadBit();
            if (!b_reserved_data_not_present)
                extractor.Skip(5);

            infoBlocks = new ObjectInfoBlock[objectCount][];
            for (int j = 0; j < objectCount; ++j) {
                infoBlocks[j] = new ObjectInfoBlock[infoBlockCount];
                for (int blk = 0; blk < infoBlockCount; ++blk)
                    infoBlocks[j][blk] = new ObjectInfoBlock(extractor, blk, j < bed_or_isf_objects);
            }
        }

        void MDUpdateInfo(BitExtractor extractor) {
            sampleOffset = extractor.Read(2) switch {
                0 => 0,
                1 => sampleOffsetIndex[extractor.Read(2)],
                2 => extractor.Read(5),
                _ => throw new UnsupportedFeatureException("mdOffset"),
            };
            infoBlockCount = extractor.Read(3) + 1;
            blockOffsetFactor = new int[infoBlockCount];
            rampDuration = new int[infoBlockCount];
            for (int blk = 0; blk < infoBlockCount; ++blk)
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

#pragma warning disable IDE0052 // Remove unread private members
        int sampleOffset;
        int infoBlockCount;
        int[] blockOffsetFactor;
        int[] rampDuration;
        readonly bool b_discard_unknown_element;
        readonly int oa_element_id_idx;
        readonly int oa_element_size;
        readonly int alternate_object_data_id_idx;
#pragma warning restore IDE0052 // Remove unread private members

        static readonly int[] sampleOffsetIndex = { 8, 16, 18, 24 };
        static readonly int[] rampDurations = { 0, 512, 1536 };
        static readonly int[] rampDurationIndex =
            { 32, 64, 128, 256, 320, 480, 1000, 1001, 1024, 1600, 1601, 1602, 1920, 2000, 2002, 2048 };

        int VariableBitsMax(BitExtractor extractor, int n, int max_num_groups) {
            int value = 0;
            int num_group = 1;
            int read = extractor.Read(n);
            value += read;
            bool b_read_more = extractor.ReadBit();
            if (max_num_groups > num_group) {
                if (b_read_more) {
                    value <<= n;
                    value += (1 << n);
                }
                while (b_read_more) {
                    read = extractor.Read(n);
                    value += read;
                    b_read_more = extractor.ReadBit();
                    if (num_group >= max_num_groups)
                        break;
                    if (b_read_more) {
                        value <<= n;
                        value += (1 << n);
                        num_group += 1;
                    }
                }
            }
            return value;
        }
    }
}