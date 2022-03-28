using System;
using System.Numerics;

using Cavern.Format.Utilities;
using Cavern.Utilities;

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

            if (b_object_in_bed_or_isf)
                anchor = ObjectAnchor.Speaker;
        }

        public void UpdatePosition(ref Vector3 pos, ref Vector3 lastPrecise) {
            if (validPosition && anchor != ObjectAnchor.Speaker) {
                if (differentialPosition)
                    pos = new Vector3(
                        QMath.Clamp(pos.X + position.X, 0, 1),
                        QMath.Clamp(pos.Y + position.Y, 0, 1),
                        QMath.Clamp(pos.Z + position.Z, 0, 1)
                    );
                else
                    lastPrecise = pos = position;

                switch (anchor) {
                    case ObjectAnchor.Room:
                        if (!float.IsNaN(objectDistance)) {
                            Vector3 intersect = pos.MapToCube();
                            float distanceFactor = intersect.Length() / objectDistance;
                            pos = distanceFactor * intersect + (1 - distanceFactor) * roomCenter;
                        }
                        break;
                    case ObjectAnchor.Screen:
                        // TODO
                        break;
                }
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

            // Spatial position
            if (validPosition = obj_render_info[3]) {
                differentialPosition = blk != 0 && extractor.ReadBit();
                if (differentialPosition)
                    position = new Vector3(
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * zScale
                    );
                else {
                    int posX = extractor.Read(6);
                    int posY = extractor.Read(6);
                    bool signZ = extractor.ReadBit();
                    int posZ = extractor.Read(4);
                    position = new Vector3(
                        Math.Min(1, posX * xyScale),
                        Math.Min(1, posY * xyScale),
                        Math.Min(1, (signZ ? 1 : -1) * posZ * zScale)
                    );
                }
                if (extractor.ReadBit()) { // Distance specified
                    if (extractor.ReadBit()) // Infinite distance
                        objectDistance = 100; // Close enough
                    else
                        objectDistance = distanceFactors[extractor.Read(4)];
                } else
                    objectDistance = float.NaN;
            }

            if (obj_render_info[2]) {
                zone_constraints_idx = extractor.Read(3);
                b_enable_elevation = extractor.ReadBit();
            }

            // Scaling
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

            // Screen anchoring
            if (obj_render_info[0] && extractor.ReadBit()) {
                anchor = ObjectAnchor.Screen;
                screen_factor = (extractor.Read(3) + 1) * .125f;
                depth_factor = depthFactors[extractor.Read(2)];
            }

            b_object_snap = extractor.ReadBit();
        }

        /// <summary>
        /// This block contained position information.
        /// </summary>
        bool validPosition;

        /// <summary>
        /// This frame contains the difference from the last object position instead of an exact position.
        /// </summary>
        bool differentialPosition;

        /// <summary>
        /// The object's anchor position.
        /// </summary>
        ObjectAnchor anchor = ObjectAnchor.Room;

        /// <summary>
        /// Object distance from the center of the room. Distant objects are mapped closer to the listening position.
        /// </summary>
        float objectDistance;

        /// <summary>
        /// The coded position information, either exact or differential.
        /// </summary>
        Vector3 position;

#pragma warning disable IDE0052 // Remove unread private members
        bool b_default_object_priority;
        bool b_enable_elevation;
        bool b_object_snap;
        bool[] object_basic_info;
        bool[] obj_render_info;
        float depth_factor;
        float screen_factor;
        int object_gain_idx;
        int object_gain_bits;
        int object_priority_bits;
        int zone_constraints_idx;
        int object_size_idx;
        int object_size_bits;
        int object_width_bits;
        int object_depth_bits;
        int object_height_bits;
        readonly bool b_object_not_active;
        readonly int object_basic_info_status_idx;
        readonly int object_render_info_status_idx;
#pragma warning restore IDE0052 // Remove unread private members

        const float xyScale = 1 / 62f;
        const float zScale = 1 / 15f;
        static readonly Vector3 roomCenter = new Vector3(.5f, .5f, 0);
        static readonly float[] distanceFactors =
            { 1.1f, 1.3f, 1.6f, 2.0f, 2.5f, 3.2f, 4.0f, 5.0f, 6.3f, 7.9f, 10.0f, 12.6f, 15.8f, 20.0f, 25.1f, 50.1f };
        static readonly float[] depthFactors = { .25f, .5f, 1, 2 };
    }
}