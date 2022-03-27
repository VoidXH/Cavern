using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    class OAElementMD {
        public OAElementMD(BitExtractor extractor, bool b_alternate_object_data_present, int object_count) {
            oa_element_id_idx = extractor.Read(4);
            oa_element_size = VariableBitsMax(extractor, 4, 4) + 1;
            int endPos = extractor.Position + oa_element_size;
            if (b_alternate_object_data_present)
                alternate_object_data_id_idx = extractor.Read(4);
            b_discard_unknown_element = extractor.ReadBit();
            ObjectElement(extractor, object_count);
            extractor.Position = endPos; // Padding
        }

        void ObjectElement(BitExtractor extractor, int object_count) {
            MDUpdateInfo(extractor);
            bool b_reserved_data_not_present = extractor.ReadBit();
            if (!b_reserved_data_not_present)
                extractor.Skip(5);
            for (int j = 0; j < object_count; ++j) {
                for (int blk = 0; blk < num_obj_info_blocks; ++blk) {
                    // TODO: ObjectInfoBlock(extractor, blk); - per object
                }
            }
        }

        void MDUpdateInfo(BitExtractor extractor) {
            sample_offset_code = extractor.Read(2);
            if (sample_offset_code == 1)
                sample_offset_idx = extractor.Read(2);
            else if (sample_offset_code == 2)
                sample_offset_bits = extractor.Read(5);

            num_obj_info_blocks = extractor.Read(3) + 1;
            block_offset_factor_bits = new int[num_obj_info_blocks];
            ramp_duration_code = new int[num_obj_info_blocks];
            b_use_ramp_duration_idx = new bool[num_obj_info_blocks];
            ramp_duration_idx = new int[num_obj_info_blocks];
            ramp_duration_bits = new int[num_obj_info_blocks];
            for (int blk = 0; blk < num_obj_info_blocks; ++blk)
                BlockUpdateInfo(extractor, blk);
        }

        void BlockUpdateInfo(BitExtractor extractor, int blk) {
            block_offset_factor_bits[blk] = extractor.Read(6);
            ramp_duration_code[blk] = extractor.Read(2);
            if (ramp_duration_code[blk] == 3) {
                b_use_ramp_duration_idx[blk] = extractor.ReadBit();
                if (b_use_ramp_duration_idx[blk])
                    ramp_duration_idx[blk] = extractor.Read(4);
                else
                    ramp_duration_bits[blk] = extractor.Read(11);
            }
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool[] b_use_ramp_duration_idx;
        int sample_offset_code;
        int sample_offset_idx;
        int sample_offset_bits;
        int num_obj_info_blocks;
        int[] block_offset_factor_bits;
        int[] ramp_duration_code;
        int[] ramp_duration_idx;
        int[] ramp_duration_bits;
        readonly bool b_discard_unknown_element;
        readonly int oa_element_id_idx;
        readonly int oa_element_size;
        readonly int alternate_object_data_id_idx;
#pragma warning restore IDE0052 // Remove unread private members

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