using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded OAMD frame from an EMDF payload.
    /// </summary>
    partial class ObjectAudioMetadata {
        /// <summary>
        /// Count of bed channels.
        /// </summary>
        public byte Beds { get; private set; }

        /// <summary>
        /// Number of audio objects in the stream. This includes both dynamic objects and <see cref="Beds"/>.
        /// </summary>
        public int ObjectCount { get; private set; }

        /// <summary>
        /// Decoded object audio element metadata.
        /// </summary>
        OAElementMD[] elements = new OAElementMD[0];

        /// <summary>
        /// Bed channels used. The first dimension is the element ID, the second is one bit for each channel,
        /// in the order of <see cref="bedChannels"/>.
        /// </summary>
        bool[][] bedAssignment;

        /// <summary>
        /// Use intermediate spatial format (ISF), which has a few fixed layouts.
        /// </summary>
        bool isfInUse;

        /// <summary>
        /// ISF layout ID.
        /// </summary>
        byte isfIndex;

        /// <summary>
        /// This payload applies this many samples later.
        /// </summary>
        int offset;

        /// <summary>
        /// Decodes a OAMD frame from an EMDF payload.
        /// </summary>
        public void Decode(BitExtractor extractor, int offset) {
            this.offset = offset;
            int versionNumber = extractor.Read(2);
            if (versionNumber == 3) {
                versionNumber += extractor.Read(3);
            }
            if (versionNumber != 0) {
                throw new UnsupportedFeatureException("OAver");
            }
            ObjectCount = extractor.Read(5) + 1;
            if (ObjectCount == 32) {
                ObjectCount += extractor.Read(7);
            }
            ProgramAssignment(extractor);
            bool alternateObjectPresent = extractor.ReadBit();
            int elementCount = extractor.Read(4);
            if (elementCount == 15) {
                elementCount += extractor.Read(5);
            }

            int bedOrISFObjects = Beds;
            if (isfInUse) {
                bedOrISFObjects += isfObjectCount[isfIndex];
            }

            if (elements.Length != elementCount) {
                elements = new OAElementMD[elementCount];
                for (int i = 0; i < elementCount; ++i) {
                    elements[i] = new OAElementMD();
                }
            }
            for (int i = 0; i < elementCount; ++i) {
                elements[i].Read(extractor, alternateObjectPresent, ObjectCount, bedOrISFObjects);
            }
        }

        /// <summary>
        /// Get the &quot;objects&quot; that are just static channels.
        /// </summary>
        public ReferenceChannel[] GetStaticChannels() {
            ReferenceChannel[] result = new ReferenceChannel[Beds];
            int lastChannel = 0;
            for (int i = 0; i < bedAssignment.Length; i++) {
                bool[] assignment = bedAssignment[i];
                for (int j = 0; j < assignment.Length; j++) {
                    if (assignment[j]) {
                        result[lastChannel] = bedChannels[j];
                        if (++lastChannel == Beds) {
                            return result;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets which object is the LFE channel or -1 if it's not present.
        /// </summary>
        public int GetLFEPosition() {
            int beds = 0;
            for (int bed = 0; bed < bedAssignment.Length; bed++) {
                for (int i = 0; i < (int)NonStandardBedChannel.Max; i++) {
                    if (bedAssignment[bed][i]) {
                        if (i == (int)NonStandardBedChannel.LowFrequencyEffects) {
                            return beds;
                        }
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
        public void UpdateSources(int timecode, IReadOnlyList<Source> sources) {
            timecode -= offset;
            int element = 0;
            for (int i = elements.Length - 1; i >= 0; --i) {
                if (elements[i].MinOffset < 0) {
                    continue;
                }
                if (elements[i].MinOffset <= timecode) {
                    element = i;
                    break;
                }
            }

            elements[element].UpdateSources(timecode, sources);
            int checkedBed = 0;
            int checkedBedInstance = 0;
            for (int i = 0; i < Beds; ++i) {
                while (!bedAssignment[checkedBedInstance][checkedBed]) {
                    if (++checkedBed == (int)NonStandardBedChannel.Max) {
                        ++checkedBedInstance;
                    }
                }
                sources[i].Position = ChannelPrototype.AlternativePositions[(int)bedChannels[checkedBed]] * Listener.EnvironmentSize;
                sources[i].LFE = ChannelPrototype.Mapping[(int)bedChannels[checkedBed]].LFE;
                if (++checkedBed == (int)NonStandardBedChannel.Max) {
                    ++checkedBedInstance;
                }
            }
        }

        void ProgramAssignment(BitExtractor extractor) {
            if (extractor.ReadBit()) { // Dynamic object-only program
                if (extractor.ReadBit()) { // LFE present
                    bedAssignment = new bool[1][];
                    bedAssignment[0] = new bool[(int)NonStandardBedChannel.Max];
                    bedAssignment[0][(int)NonStandardBedChannel.LowFrequencyEffects] = true;
                } else {
                    bedAssignment = new bool[0][];
                }
            } else {
                int contentDescription = extractor.Read(4);

                // Object(s) with speaker-anchored coordinate(s) (bed objects)
                if ((contentDescription & 1) != 0) {
                    extractor.Skip(1); // The object is distributable - Cavern will do it anyway
                    bedAssignment = new bool[extractor.ReadBit() ? extractor.Read(3) + 2 : 1][];
                    for (int bed = 0; bed < bedAssignment.Length; ++bed) {
                        bedAssignment[bed] = new bool[(int)NonStandardBedChannel.Max];
                        if (extractor.ReadBit()) { // LFE only
                            bedAssignment[bed][(int)NonStandardBedChannel.LowFrequencyEffects] = true;
                        } else {
                            if (extractor.ReadBit()) { // Standard bed assignment
                                bool[] standardAssignment = extractor.ReadBits(10);
                                for (int i = 0; i < standardAssignment.Length; ++i) {
                                    for (int j = 0; j < standardBedChannels[i].Length; ++j) {
                                        bedAssignment[bed][standardBedChannels[i][j]] = standardAssignment[i];
                                    }
                                }
                            } else {
                                bedAssignment[bed] = extractor.ReadBits((int)NonStandardBedChannel.Max);
                            }
                        }
                    }
                }

                // Intermediate spatial format (ISF)
                if (isfInUse = (contentDescription & 2) != 0) {
                    isfIndex = (byte)extractor.Read(3);
                    if (isfIndex >= isfObjectCount.Length) {
                        throw new UnsupportedFeatureException("ISF");
                    }
                }

                // Object(s) with room-anchored or screen-anchored coordinates
                if ((contentDescription & 4) != 0) { // This is useless, same as ObjectCount - bedOrISFObjects, also found in JOC
                    if (extractor.Read(5) == 31) {
                        extractor.Skip(7);
                    }
                }

                // Reserved
                if ((contentDescription & 8) != 0) {
                    extractor.Skip((extractor.Read(4) + 1) * 8);
                }
            }

            Beds = 0;
            for (int bed = 0; bed < bedAssignment.Length; ++bed) {
                for (int i = 0; i < (int)NonStandardBedChannel.Max; ++i) {
                    if (bedAssignment[bed][i]) {
                        ++Beds;
                    }
                }
            }
        }

        static readonly byte[] isfObjectCount = { 4, 8, 10, 14, 15, 30 };

        /// <summary>
        /// What each bit of <see cref="bedAssignment"/> means.
        /// </summary>
        static readonly ReferenceChannel[] bedChannels = {
            ReferenceChannel.FrontLeft,
            ReferenceChannel.FrontRight,
            ReferenceChannel.FrontCenter,
            ReferenceChannel.ScreenLFE,
            ReferenceChannel.SideLeft,
            ReferenceChannel.SideRight,
            ReferenceChannel.RearLeft,
            ReferenceChannel.RearRight,
            ReferenceChannel.TopFrontLeft,
            ReferenceChannel.TopFrontRight,
            ReferenceChannel.TopSideLeft,
            ReferenceChannel.TopSideRight,
            ReferenceChannel.TopRearLeft,
            ReferenceChannel.TopRearRight,
            ReferenceChannel.WideLeft,
            ReferenceChannel.WideRight,
            ReferenceChannel.ScreenLFE,
        };

        /// <summary>
        /// Which <see cref="bedChannels"/> are set with each bit of a standard layout.
        /// </summary>
        static readonly byte[][] standardBedChannels = {
            new byte[] { 0, 1 },
            new byte[] { 2 },
            new byte[] { 3 },
            new byte[] { 4, 5 },
            new byte[] { 6, 7 },
            new byte[] { 8, 9 },
            new byte[] { 10, 11 },
            new byte[] { 12, 13 },
            new byte[] { 14, 15 },
            new byte[] { 16 }
        };
    }
}