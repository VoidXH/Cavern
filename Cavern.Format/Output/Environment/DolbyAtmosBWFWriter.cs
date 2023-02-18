using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.SpecialSources;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Dolby Atmos has a master format that is a subset of ADM BWF with certain restrictions.
    /// 10 bed channels always have to be present, even if they are empty.
    /// </summary>
    public class DolbyAtmosBWFWriter : BroadcastWaveFormatWriter {
        /// <summary>
        /// ADM BWF exporter with Dolby Atmos compatibility options.
        /// </summary>
        /// <param name="writer">File output stream</param>
        /// <param name="source">Rendering environment that should be exported</param>
        /// <param name="length">Total samples to write</param>
        /// <param name="bits">Bit depth of the output</param>
        /// <param name="staticObjects">Objects that should be exported as a bed channel if possible</param>
        public DolbyAtmosBWFWriter(Stream writer, Listener source, long length, BitDepth bits,
            (ReferenceChannel, Source)[] staticObjects) :
            base(writer, ExtendWithMuteTarget(source, staticObjects), length, bits) { }

        /// <summary>
        /// Calling this for the base constructor is a shortcut to adding extra tracks which are wired as the required bed.
        /// Additionally, <paramref name="staticObjects"/> could be mapped to the bad if a corresponding bed channel exists.
        /// </summary>
        static Listener ExtendWithMuteTarget(Listener source, (ReferenceChannel, Source)[] staticObjects) {
            for (int i = bedChannels.Length - 1; i >= 0; i--) {
                bool attached = false;
                for (int j = 0; j < staticObjects.Length; j++) {
                    if (bedChannels[i] == (int)staticObjects[j].Item1) {
                        source.AttachPrioritySource(staticObjects[j].Item2);
                        attached = true;
                        break;
                    }
                }
                if (!attached) {
                    Source mute = new MuteSource(source);
                    source.AttachPrioritySource(mute);
                }
            }
            return source;
        }

        /// <summary>
        /// ADM BWF exporter with Dolby Atmos compatibility options.
        /// </summary>
        /// <param name="path">File output path</param>
        /// <param name="source">Rendering environment that should be exported</param>
        /// <param name="length">Total samples to write</param>
        /// <param name="bits">Bit depth of the output</param>
        /// <param name="staticObjects">Objects that should be exported as a bed channel if possible</param>
        public DolbyAtmosBWFWriter(string path, Listener source, long length, BitDepth bits, (ReferenceChannel, Source)[] staticObjects) :
            this(AudioWriter.Open(path), source, length, bits, staticObjects) { }

        /// <summary>
        /// Generates the ADM structure from the recorded movement and wires the mute channel to beds.
        /// </summary>
        protected override AudioDefinitionModel CreateModel() {
            ADMTimeSpan contentLength = GetContentLength();
            const string bedContentID = "ACO_1001",
                objectContentID = "ACO_1002",
                bedObjectID = "AO_1001",
                bedPackFormatID = "AP_00011001";
            List<string> objectIDs = new List<string>();

            List<ADMProgramme> programs = new List<ADMProgramme> {
                new ADMProgramme("APR_1001", "Cavern_Export", contentLength) {
                    Contents = new List<string> { bedContentID, objectContentID }
                }
            };

            List<ADMContent> contents = new List<ADMContent> {
                new ADMContent(bedContentID, "Cavern_Master_Content") {
                    Objects = new List<string> { bedObjectID }
                },
                new ADMContent(objectContentID, "Objects") {
                    Objects = objectIDs
                }
            };

            ADMObject bedObject = new ADMObject(bedObjectID, "Bed", default, contentLength, bedPackFormatID) {
                Tracks = new List<string>()
            };
            List<ADMObject> objects = new List<ADMObject> { bedObject };

            ADMPackFormat bedPackFormat = new ADMPackFormat(bedPackFormatID, "CavernCustomPackFormat1", ADMPackType.DirectSpeakers) {
                ChannelFormats = new List<string>()
            };
            List<ADMPackFormat> packFormats = new List<ADMPackFormat> { bedPackFormat };
            List<ADMChannelFormat> channelFormats = new List<ADMChannelFormat>();
            List<ADMTrack> tracks = new List<ADMTrack>();
            List<ADMTrackFormat> trackFormats = new List<ADMTrackFormat>();
            List<ADMStreamFormat> streamFormats = new List<ADMStreamFormat>();

            string packHex = ((int)ADMPackType.DirectSpeakers).ToString("x4");
            for (int i = 1; i <= bedChannels.Length; i++) {
                string trackID = "ATU_" + i.ToString("x8");
                string trackFormatID = $"AT_{packHex}{0x1000 + i:x4}_01";
                string channelFormatID = $"AC_{packHex}{0x1000 + i:x4}";
                string streamFormatID = $"AS_{packHex}{0x1000 + i:x4}";
                string channelName = ADMConsts.channelNames[bedChannels[i - 1]];

                bedObject.Tracks.Add(trackID);
                bedPackFormat.ChannelFormats.Add(channelFormatID);
                tracks.Add(new ADMTrack(trackID, output.Bits, output.SampleRate, trackFormatID, bedPackFormatID));
                trackFormats.Add(new ADMTrackFormat(trackFormatID, "PCM_" + channelName, ADMTrackCodec.PCM, streamFormatID));
                streamFormats.Add(new ADMStreamFormat(streamFormatID, "PCM_" + channelName, ADMTrackCodec.PCM,
                    channelFormatID, bedPackFormatID, trackFormatID));

                channelFormats.Add(new ADMChannelFormat(channelFormatID, channelName, ADMPackType.DirectSpeakers) {
                    Blocks = new List<ADMBlockFormat> {
                        new ADMBlockFormat {
                            Position = ChannelPrototype.AlternativePositions[bedChannels[i - 1]]
                        }
                    }
                });
            }

            for (int i = 0; i < movements.Length; i++) {
                if (movements[i].Count != 0) {
                    FixEndTimings(movements[i], contentLength);
                } else {
                    movements[i].Add(new ADMBlockFormat {
                        Duration = contentLength
                    });
                }
            }

            packHex = ((int)ADMPackType.Objects).ToString("x4");
            for (int i = 0; i < movements.Length - bedChannels.Length; i++) {
                string id = (0x1001 + i).ToString("x4"),
                    totalId = (0x1001 + bedChannels.Length + i).ToString("x4"),
                    objectID = "AO_" + totalId,
                    objectName = "Cavern_Obj_" + (i + 1),
                    packFormatID = $"AP_{packHex}{id}",
                    channelFormatID = $"AC_{packHex}{id}",
                    trackID = "ATU_0000" + (i + bedChannels.Length + 1).ToString("x4"),
                    trackFormatID = $"AT_{packHex}{id}_01",
                    streamFormatID = $"AS_{packHex}{id}";

                objectIDs.Add(objectID);
                objects.Add(new ADMObject(objectID, "Audio Object " + (i + 1), default, contentLength, packFormatID) {
                    Tracks = new List<string> { trackID }
                });
                packFormats.Add(new ADMPackFormat(packFormatID, objectName, ADMPackType.Objects) {
                    ChannelFormats = new List<string> { channelFormatID }
                });
                channelFormats.Add(new ADMChannelFormat(channelFormatID, objectName, ADMPackType.Objects) {
                    Blocks = movements[i + bedChannels.Length]
                });
                tracks.Add(new ADMTrack(trackID, output.Bits, output.SampleRate, trackFormatID, packFormatID));
                trackFormats.Add(new ADMTrackFormat(trackFormatID, "PCM_" + objectName, ADMTrackCodec.PCM, streamFormatID));
                streamFormats.Add(new ADMStreamFormat(streamFormatID, "PCM_" + objectName, ADMTrackCodec.PCM,
                    channelFormatID, packFormatID, trackFormatID));
            }

            contents.RemoveAll(content => content.Objects.Count == 0);

            return new AudioDefinitionModel(programs, contents, objects, packFormats, channelFormats,
                tracks, trackFormats, streamFormats);
        }

        /// <summary>
        /// Add Dolby audio Metadata to Atmos BWF files.
        /// </summary>
        protected override void WriteAdditionalChunks() =>
            output.WriteChunk(RIFFWave.dbmdSync, new DolbyMetadata((byte)output.ChannelCount).Serialize());

        /// <summary>
        /// Indexes of Dolby Atmos beds (7.1.2) in the <see cref="ADMConsts.channelNames"/> array.
        /// </summary>
        static readonly byte[] bedChannels = { 0, 1, 2, 3, 6, 7, 4, 5, 17, 18 };
    }
}