using System.Numerics;

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded OAMD frame from an EMDF payload.
    /// </summary>
    class ObjectAudioMetadata {
        /// <summary>
        /// Number of audio objects in the stream.
        /// </summary>
        public int ObjectCount { get; private set; }

        /// <summary>
        /// Decoded object audio element metadata.
        /// </summary>
        readonly OAElementMD[] elements;

        /// <summary>
        /// Bed channels used. The first dimension is the element ID, the second is one bit for each channel,
        /// in the order of <see cref="bedChannels"/>.
        /// </summary>
        bool[][] bedAssignment;

        /// <summary>
        /// Count of bed channels.
        /// </summary>
        int beds;

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
            ObjectCount = extractor.Read(5);
            if (ObjectCount == 31)
                ObjectCount = extractor.Read(7);
            ++ObjectCount;
            ProgramAssignment(extractor);
            bool alternateObjectPresent = extractor.ReadBit();
            int elementCount = extractor.Read(4);
            if (elementCount == 15)
                elementCount += extractor.Read(5);
            elements = new OAElementMD[elementCount];
            for (int i = 0; i < elementCount; ++i)
                elements[i] = new OAElementMD(extractor, alternateObjectPresent, ObjectCount, bed_or_isf_objects);
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

            Vector3[] result = elements[element].GetPositions();
            int checkedBed = 0;
            for (int i = 0; i < beds; ++i) {
                while (!bedAssignment[element][checkedBed])
                    ++checkedBed;
                ChannelPrototype prototype = ChannelPrototype.Mapping[(int)bedChannels[checkedBed]];
                result[i] = new Vector3(prototype.X, prototype.Y, 0).PlaceInCube() * Listener.EnvironmentSize;
            }
            return result;
        }

        void ProgramAssignment(BitExtractor extractor) {
            b_dyn_object_only_program = extractor.ReadBit();
            if (b_dyn_object_only_program) {
                b_lfe_present = extractor.ReadBit();
                bedAssignment = new bool[num_bed_instances = 1][];
                bedAssignment[0] = new bool[(int)NonStandardBedChannel.Max];
                bedAssignment[0][(int)NonStandardBedChannel.LowFrequencyEffects] = true;
            } else {
                content_description = extractor.ReadBits(4);

                // Object(s) with speaker-anchored coordinate(s) (bed objects)
                if (content_description[3]) {
                    b_bed_chan_distribute = extractor.ReadBit();
                    if (b_multiple_bed_instances_present = extractor.ReadBit())
                        num_bed_instances = extractor.Read(3) + 2;
                    else
                        num_bed_instances = 1;
                    bedAssignment = new bool[num_bed_instances][];
                    for (int bed = 0; bed < num_bed_instances; ++bed) {
                        bool b_lfe_only = extractor.ReadBit();
                        if (b_lfe_only) {
                            bedAssignment[bed] = new bool[(int)NonStandardBedChannel.Max];
                            bedAssignment[bed][(int)NonStandardBedChannel.LowFrequencyEffects] = true;
                        } else {
                            if (extractor.ReadBit()) { // Standard bed assignment
                                bool[] standardAssignment = extractor.ReadBits(10);
                                for (int i = 0; i < standardAssignment.Length; ++i)
                                    for (int j = 0; j < standardBedChannels[i].Length; ++j)
                                        bedAssignment[bed][standardBedChannels[i][j]] = standardAssignment[i];
                            } else
                                bedAssignment[bed] = extractor.ReadBits((int)NonStandardBedChannel.Max);
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

            beds = 0;
            for (int bed = 0; bed < num_bed_instances; ++bed) {
                for (int i = 0; i < (int)NonStandardBedChannel.Max; ++i)
                    if (bedAssignment[bed][i])
                        ++beds;
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
        int num_bed_instances;
        int intermediate_spatial_format_idx;
        int bed_or_isf_objects;
        int num_dynamic_objects_bits;
#pragma warning restore IDE0052 // Remove unread private members

        static readonly byte[] isf_objects = { 4, 8, 10, 14, 15, 30 };

        /// <summary>
        /// What each bit of <see cref="bedAssignment"/> means.
        /// </summary>
        static readonly ReferenceChannel[] bedChannels = {
            ReferenceChannel.ScreenLFE,
            ReferenceChannel.WideRight,
            ReferenceChannel.WideLeft,
            ReferenceChannel.TopRearRight,
            ReferenceChannel.TopRearLeft,
            ReferenceChannel.TopSideRight,
            ReferenceChannel.TopSideLeft,
            ReferenceChannel.TopFrontRight,
            ReferenceChannel.TopFrontLeft,
            ReferenceChannel.RearRight,
            ReferenceChannel.RearLeft,
            ReferenceChannel.SideRight,
            ReferenceChannel.SideLeft,
            ReferenceChannel.ScreenLFE,
            ReferenceChannel.FrontCenter,
            ReferenceChannel.FrontRight,
            ReferenceChannel.FrontLeft
        };

        /// <summary>
        /// Which <see cref="bedChannels"/> are set with each bit of a standard layout.
        /// </summary>
        static readonly byte[][] standardBedChannels = {
            new byte[] { 0 },
            new byte[] { 1, 2 },
            new byte[] { 3, 4 },
            new byte[] { 5, 6 },
            new byte[] { 7, 8 },
            new byte[] { 9, 10 },
            new byte[] { 11, 12 },
            new byte[] { 13 },
            new byte[] { 14 },
            new byte[] { 15, 16 }
        };
    }
}