using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    class ObjectInfoBlock {
        public ObjectInfoBlock(BitExtractor extractor, int blk, bool b_object_in_bed_or_isf) {
            b_object_not_active = extractor.ReadBit();
            if (b_object_not_active)
                object_basic_info_status_idx = 0;
            else
                object_basic_info_status_idx = blk == 0 ? 1 : extractor.Read(2);
            if ((object_basic_info_status_idx == 1) || (object_basic_info_status_idx == 3))
                ObjectBasicInfo(extractor);
            if (b_object_not_active)
                object_render_info_status_idx = 0;
            else if (!b_object_in_bed_or_isf)
                object_render_info_status_idx = blk == 0 ? 1 : extractor.Read(2);
            else
                object_render_info_status_idx = 0;
            if ((object_render_info_status_idx == 1) || (object_render_info_status_idx == 3))
                ObjectRenderInfo(extractor, blk);
            bool b_additional_table_data_exists = extractor.ReadBit();
            if (b_additional_table_data_exists) {
                int additional_table_data_size = extractor.Read(4) + 1;
                extractor.Skip(additional_table_data_size * 8);
            }
        }

        void ObjectBasicInfo(BitExtractor extractor) {
            object_basic_info = new bool[2];
            if (object_basic_info_status_idx == 1)
                object_basic_info = new bool[] { true, true };
            else
                object_basic_info = extractor.ReadBits(2);
            if (object_basic_info[1]) {
                object_gain_idx = extractor.Read(2);
                if (object_gain_idx == 2)
                    object_gain_bits = extractor.Read(6);
            }
            if (object_basic_info[0]) {
                b_default_object_priority = extractor.ReadBit();
                if (!b_default_object_priority)
                    object_priority_bits = extractor.Read(5);
            }
        }

        void ObjectRenderInfo(BitExtractor extractor, int blk) {
            if (object_render_info_status_idx == 1)
                obj_render_info = new bool[] { true, true, true, true };
            else
                obj_render_info = extractor.ReadBits(4);
            if (obj_render_info[3]) {
                b_differential_position_specified = blk != 0 && extractor.ReadBit();
                if (b_differential_position_specified) {
                    diff_pos3D_X_bits = extractor.Read(3);
                    diff_pos3D_Y_bits = extractor.Read(3);
                    diff_pos3D_Z_bits = extractor.Read(3);
                } else {
                    pos3D_X_bits = extractor.Read(6);
                    pos3D_Y_bits = extractor.Read(6);
                    pos3D_Z_sign_bits = extractor.ReadBit();
                    pos3D_Z_bits = extractor.Read(4);
                }
                b_object_distance_specified = extractor.ReadBit();
                if (b_object_distance_specified) {
                    b_object_at_infinity = extractor.ReadBit();
                    if (b_object_at_infinity)
                        object_distance = float.PositiveInfinity;
                    else
                        distance_factor = distance_factors[extractor.Read(4)];
                }
            }
            if (obj_render_info[2]) {
                zone_constraints_idx = extractor.Read(3);
                b_enable_elevation = extractor.ReadBit();
            }
            if (obj_render_info[1]) {
                object_size_idx = extractor.Read(2);
                if (object_size_idx == 1)
                    object_size_bits = extractor.Read(5);
                else {
                    if (object_size_idx == 2) {
                        object_width_bits = extractor.Read(5);
                        object_depth_bits = extractor.Read(5);
                        object_height_bits = extractor.Read(5);
                    }
                }
            }
            if (obj_render_info[0]) {
                b_object_use_screen_ref = extractor.ReadBit();
                if (b_object_use_screen_ref) {
                    screen_factor_bits = extractor.Read(3);
                    depth_factor_idx = extractor.Read(2);
                } else
                    screen_factor_bits = 0;
            }
            b_object_snap = extractor.ReadBit();
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool b_default_object_priority;
        bool b_differential_position_specified;
        bool pos3D_Z_sign_bits;
        bool b_object_distance_specified;
        bool b_object_at_infinity;
        bool b_enable_elevation;
        bool b_object_use_screen_ref;
        bool b_object_snap;
        bool[] object_basic_info;
        bool[] obj_render_info;
        float object_distance;
        int object_gain_idx;
        int object_gain_bits;
        int object_priority_bits;
        int diff_pos3D_X_bits;
        int diff_pos3D_Y_bits;
        int diff_pos3D_Z_bits;
        int pos3D_X_bits;
        int pos3D_Y_bits;
        int pos3D_Z_bits;
        int zone_constraints_idx;
        int object_size_idx;
        int object_size_bits;
        int object_width_bits;
        int object_depth_bits;
        int object_height_bits;
        int screen_factor_bits;
        int depth_factor_idx;
        float distance_factor;
        readonly bool b_object_not_active;
        readonly int object_basic_info_status_idx;
        readonly int object_render_info_status_idx;
#pragma warning restore IDE0052 // Remove unread private members

        static readonly float[] distance_factors =
            { 1.1f, 1.3f, 1.6f, 2.0f, 2.5f, 3.2f, 4.0f, 5.0f, 6.3f, 7.9f, 10.0f, 12.6f, 15.8f, 20.0f, 25.1f, 50.1f };
    }
}