using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded OAMD frame from an EMDF payload.
    /// </summary>
    class ObjectAudioMetadata {
        /// <summary>
        /// Decoded object audio element metadata.
        /// </summary>
        public IReadOnlyList<OAElementMD> Elements => elements;

        /// <summary>
        /// Decoded object audio element metadata.
        /// </summary>
        readonly OAElementMD[] elements;

        /// <summary>
        /// Decodes a OAMD frame from an EMDF payload.
        /// </summary>
        public ObjectAudioMetadata(ExtensibleMetadataPayload payload) {
            BitExtractor extractor = new BitExtractor(payload.Payload);
            int oa_md_version_bits = extractor.Read(2);
            if (oa_md_version_bits == 3)
                oa_md_version_bits += extractor.Read(3);
            if (oa_md_version_bits != 0)
                throw new UnsupportedFeatureException("OAver");
            object_count = extractor.Read(5);
            if (object_count == 31)
                object_count = extractor.Read(7);
            ++object_count;
            ProgramAssignment(extractor);
            b_alternate_object_data_present = extractor.ReadBit();
            int oa_element_count_bits = extractor.Read(4);
            if (oa_element_count_bits == 15)
                oa_element_count_bits += extractor.Read(5);
            elements = new OAElementMD[oa_element_count_bits];
            for (int i = 0; i < oa_element_count_bits; ++i)
                elements[i] = new OAElementMD(extractor, b_alternate_object_data_present, object_count);
        }

        void ProgramAssignment(BitExtractor extractor) {
            b_dyn_object_only_program = extractor.ReadBit();
            if (b_dyn_object_only_program)
                b_lfe_present = extractor.ReadBit();
            else {
                content_description = extractor.ReadBits(4);

                // Object(s) with speaker-anchored coordinate(s) (bed objects)
                if (content_description[3]) {
                    b_bed_chan_distribute = extractor.ReadBit();
                    if (b_multiple_bed_instances_present = extractor.ReadBit())
                        num_bed_instances = extractor.Read(3) + 2;
                    else
                        num_bed_instances = 1;
                    b_standard_chan_assign = new bool[num_bed_instances];
                    bed_channel_assignment = new bool[num_bed_instances][];
                    nonstd_bed_channel_assignment = new bool[num_bed_instances][];
                    for (int bed = 0; bed < num_bed_instances; ++bed) {
                        bool b_lfe_only = extractor.ReadBit();
                        if (b_lfe_only) {
                            bed_channel_assignment[bed] = new bool[(int)OAMDBedChannel.Max];
                            bed_channel_assignment[bed][(int)OAMDBedChannel.RC_LFE] = true;
                        } else {
                            b_standard_chan_assign[bed] = extractor.ReadBit();
                            if (b_standard_chan_assign[bed])
                                bed_channel_assignment[bed] = extractor.ReadBits(10);
                            else
                                nonstd_bed_channel_assignment[bed] = extractor.ReadBits(17);
                        }
                    }
                }

                // Intermediate spatial format (ISF)
                if (content_description[2])
                    intermediate_spatial_format_idx = extractor.Read(3);

                // Object(s) with room-anchored or screen-anchored coordinates
                if (content_description[1]) {
                    num_dynamic_objects_bits = extractor.Read(5);
                    if (num_dynamic_objects_bits == 31)
                        num_dynamic_objects_bits += extractor.Read(7);
                }

                // Reserved
                if (content_description[0]) {
                    int reserved_data_size = extractor.Read(4) + 1;
                    extractor.Skip(reserved_data_size * 8);
                }
            }
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool b_dyn_object_only_program;
        bool b_lfe_present;
        bool b_bed_chan_distribute;
        bool b_multiple_bed_instances_present;
        bool[] content_description;
        bool[] b_standard_chan_assign;
        bool[][] bed_channel_assignment;
        bool[][] nonstd_bed_channel_assignment;
        int num_bed_instances;
        int intermediate_spatial_format_idx;
        int num_dynamic_objects_bits;
        readonly bool b_alternate_object_data_present;
        readonly int object_count;
#pragma warning restore IDE0052 // Remove unread private members
    }
}