using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded OAMD frame from an EMDF payload.
    /// </summary>
    class ObjectAudioMetadata {
        /// <summary>
        /// Number of audio objects in the stream.
        /// </summary>
        readonly int objectCount;

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
            objectCount = extractor.Read(5);
            if (objectCount == 31)
                objectCount = extractor.Read(7);
            ++objectCount;
            ProgramAssignment(extractor);
            b_alternate_object_data_present = extractor.ReadBit();
            int elementCount = extractor.Read(4);
            if (elementCount == 15)
                elementCount += extractor.Read(5);
            elements = new OAElementMD[elementCount];
            for (int i = 0; i < elementCount; ++i)
                elements[i] = new OAElementMD(extractor, b_alternate_object_data_present, objectCount, bed_or_isf_objects);
        }

        /// <summary>
        /// Get the spatial position of each object.
        /// </summary>
        /// <param name="timecode">Samples since the beginning of the audio frame</param>
        public Vector3[] GetPositions(int timecode) {
            int element = 0;
            for (int i = elements.Length - 1; i >= 0; --i) {
                if (elements[i].MinOffset <= timecode) {
                    element = i;
                    break;
                }
            }
            // TODO: handle ramps
            return elements[element].GetPositions();
        }

        void ProgramAssignment(BitExtractor extractor) {
            b_dyn_object_only_program = extractor.ReadBit();
            if (b_dyn_object_only_program) {
                b_lfe_present = extractor.ReadBit();
                b_standard_chan_assign = new bool[] { true };
                bed_channel_assignment = new bool[num_bed_instances = 1][];
                bed_channel_assignment[0] = new bool[(int)OAMDBedChannel.Max];
                bed_channel_assignment[0][(int)OAMDBedChannel.LowFrequencyEffects] = true;
            } else {
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
                            b_standard_chan_assign[bed] = true;
                            bed_channel_assignment[bed] = new bool[(int)OAMDBedChannel.Max];
                            bed_channel_assignment[bed][(int)OAMDBedChannel.LowFrequencyEffects] = true;
                        } else {
                            b_standard_chan_assign[bed] = extractor.ReadBit();
                            if (b_standard_chan_assign[bed])
                                bed_channel_assignment[bed] = extractor.ReadBits(10);
                            else
                                nonstd_bed_channel_assignment[bed] = extractor.ReadBits((int)NonStandardBedChannel.Max);
                        }
                    }
                }

                // Intermediate spatial format (ISF)
                if (use_isf = content_description[2]) {
                    intermediate_spatial_format_idx = extractor.Read(3);
                    if (intermediate_spatial_format_idx >= isf_objects.Length)
                        throw new UnsupportedFeatureException("ISF");
                }

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

            int beds = 0;
            for (int bed = 0; bed < num_bed_instances; ++bed) {
                if (b_standard_chan_assign[bed]) {
                    for (int i = 0; i < (int)OAMDBedChannel.Max; ++i)
                        if (bed_channel_assignment[bed][i])
                            beds += standardBedChannels[i];
                } else {
                    for (int i = 0; i < (int)NonStandardBedChannel.Max; ++i)
                        if (nonstd_bed_channel_assignment[bed][i])
                            ++beds;
                }
            }
            bed_or_isf_objects = beds;
            if (use_isf)
                bed_or_isf_objects += isf_objects[intermediate_spatial_format_idx];
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool b_dyn_object_only_program;
        bool b_lfe_present;
        bool b_bed_chan_distribute;
        bool b_multiple_bed_instances_present;
        bool use_isf;
        bool[] content_description;
        bool[] b_standard_chan_assign;
        bool[][] bed_channel_assignment;
        bool[][] nonstd_bed_channel_assignment;
        int num_bed_instances;
        int intermediate_spatial_format_idx;
        int bed_or_isf_objects;
        int num_dynamic_objects_bits;
        readonly bool b_alternate_object_data_present;
#pragma warning restore IDE0052 // Remove unread private members

        static readonly int[] standardBedChannels = { 1, 2, 2, 2, 2, 2, 2, 1, 1, 2 };
        static readonly int[] isf_objects = { 4, 8, 10, 14, 15, 30 };
    }
}