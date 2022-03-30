using System.Collections.Generic;
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
        public byte ObjectCount { get; private set; }

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
        byte beds;

        /// <summary>
        /// Use intermediate spatial format (ISF), which has a few fixed layouts.
        /// </summary>
        bool isfInUse;

        /// <summary>
        /// ISF layout ID.
        /// </summary>
        byte isfIndex;

        /// <summary>
        /// Decodes a OAMD frame from an EMDF payload.
        /// </summary>
        public ObjectAudioMetadata(ExtensibleMetadataPayload payload) {
            BitExtractor extractor = new BitExtractor(payload.Payload);
            int versionNumber = extractor.Read(2);
            if (versionNumber == 3)
                versionNumber += extractor.Read(3);
            if (versionNumber != 0)
                throw new UnsupportedFeatureException("OAver");
            ObjectCount = (byte)extractor.Read(5);
            if (ObjectCount == 31)
                ObjectCount += (byte)extractor.Read(7);
            ++ObjectCount;
            ProgramAssignment(extractor);
            bool alternateObjectPresent = extractor.ReadBit();
            int elementCount = extractor.Read(4);
            if (elementCount == 15)
                elementCount += extractor.Read(5);

            int bedOrISFObjects = beds;
            if (isfInUse)
                bedOrISFObjects += isfObjectCount[isfIndex];
            elements = new OAElementMD[elementCount];
            for (int i = 0; i < elementCount; ++i)
                elements[i] = new OAElementMD(extractor, alternateObjectPresent, ObjectCount, bedOrISFObjects);
        }

        /// <summary>
        /// Gets which object is the LFE channel or -1 if it's not present.
        /// </summary>
        public int GetLFEPosition() {
            int beds = 0;
            for (int bed = 0; bed < bedAssignment.Length; ++bed) {
                for (int i = 0; i < (int)NonStandardBedChannel.Max; ++i) {
                    if (bedAssignment[bed][i]) {
                        if (i == (int)NonStandardBedChannel.LowFrequencyEffects)
                            return beds;
                        ++beds;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Set the object properties from metadata.
        /// </summary>
        /// <param name="timecode">Samples since the beginning of the audio frame</param>
        /// <param name="sources">The sources used for rendering this track</param>
        /// <param name="lastHoldPos">A helper array the size of <see cref="ObjectCount"/></param>
        public void UpdateSources(int timecode, IReadOnlyList<Source> sources, Vector3[] lastHoldPos) {
            int element = 0;
            for (int i = elements.Length - 1; i >= 0; --i) {
                if (elements[i].MinOffset <= timecode) {
                    element = i;
                    break;
                }
            }

            elements[element].UpdateSources(timecode, sources, lastHoldPos);
            int checkedBed = 0;
            int checkedBedInstance = 0;
            for (int i = 0; i < beds; ++i) {
                while (!bedAssignment[checkedBedInstance][checkedBed]) {
                    ++checkedBed;
                    if (checkedBed == (int)NonStandardBedChannel.Max)
                        ++checkedBedInstance;
                }
                ChannelPrototype prototype = ChannelPrototype.Mapping[(int)bedChannels[checkedBed]];
                sources[i].Position = new Vector3(prototype.X, prototype.Y, 0).PlaceInCube() * Listener.EnvironmentSize;
                sources[i].LFE = prototype.LFE;
            }
        }

        void ProgramAssignment(BitExtractor extractor) {
            if (extractor.ReadBit()) { // Dynamic object-only program
                if (extractor.ReadBit()) { // LFE present
                    bedAssignment = new bool[1][];
                    bedAssignment[0] = new bool[(int)NonStandardBedChannel.Max];
                    bedAssignment[0][(int)NonStandardBedChannel.LowFrequencyEffects] = true;
                }
            } else {
                bool[] contentDescription = extractor.ReadBits(4);

                // Object(s) with speaker-anchored coordinate(s) (bed objects)
                if (contentDescription[3]) {
                    extractor.Skip(1); // The object is distributable - Cavern will do it anyway
                    bedAssignment = new bool[extractor.ReadBit() ? extractor.Read(3) + 2 : 1][];
                    for (int bed = 0; bed < bedAssignment.Length; ++bed) {
                        if (extractor.ReadBit()) { // LFE only
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
                if (isfInUse = contentDescription[2]) {
                    isfIndex = (byte)extractor.Read(3);
                    if (isfIndex >= isfObjectCount.Length)
                        throw new UnsupportedFeatureException("ISF");
                }

                // Object(s) with room-anchored or screen-anchored coordinates
                if (contentDescription[1]) // This is useless, same as ObjectCount - bedOrISFObjects, also found in JOC
                    if (extractor.Read(5) == 31)
                        extractor.Skip(7);

                // Reserved
                if (contentDescription[0])
                    extractor.Skip((extractor.Read(4) + 1) * 8);
            }

            beds = 0;
            for (int bed = 0; bed < bedAssignment.Length; ++bed)
                for (int i = 0; i < (int)NonStandardBedChannel.Max; ++i)
                    if (bedAssignment[bed][i])
                        ++beds;
        }

        static readonly byte[] isfObjectCount = { 4, 8, 10, 14, 15, 30 };

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