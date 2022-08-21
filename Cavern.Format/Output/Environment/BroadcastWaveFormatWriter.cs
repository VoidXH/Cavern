using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Xml;

using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Utilities;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
    /// </summary>
    public class BroadcastWaveFormatWriter : EnvironmentWriter {
        /// <summary>
        /// The main PCM exporter.
        /// </summary>
        readonly RIFFWaveWriter output;

        /// <summary>
        /// Metadata of rendered sources to export to the end of the file.
        /// </summary>
        readonly AudioDefinitionModel adm;

        /// <summary>
        /// Recorded movement path of all sources.
        /// </summary>
        readonly List<ADMBlockFormat>[] movements;

        /// <summary>
        /// Total samples written to the export file.
        /// </summary>
        long samplesWritten;

        /// <summary>
        /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
        /// </summary>
        public BroadcastWaveFormatWriter(BinaryWriter writer, Listener source, long length, BitDepth bits) :
            base(writer, source) {
            output = new RIFFWaveWriter(writer, source.ActiveSources.Count, length, source.SampleRate, bits) {
                MaxLargeChunks = 1
            };
            output.WriteHeader();

            movements = new List<ADMBlockFormat>[output.ChannelCount];
            for (int i = 0; i < output.ChannelCount; i++) {
                movements[i] = new List<ADMBlockFormat>();
            }

            double contentLength = length / (double)source.SampleRate;
            TimeSpan contentTime = TimeSpan.FromSeconds(contentLength);
            string contentID = "ACO_1001";
            List<string> objectIDs = new List<string>();

            List<ADMProgramme> programs = new List<ADMProgramme> {
                new ADMProgramme("APR_1001", "Cavern_Export", contentLength) {
                    Contents = new List<string>() { contentID }
                }
            };
            List<ADMContent> contents = new List<ADMContent> {
                new ADMContent(contentID, "Objects") {
                    Objects = objectIDs
                }
            };
            List<ADMObject> objects = new List<ADMObject>();
            List<ADMPackFormat> packFormats = new List<ADMPackFormat>();
            List<ADMChannelFormat> channelFormats = new List<ADMChannelFormat>();
            List<ADMTrack> tracks = new List<ADMTrack>();
            List<ADMTrackFormat> trackFormats = new List<ADMTrackFormat>();
            List<ADMStreamFormat> streamFormats = new List<ADMStreamFormat>();

            int sourceIndex = 0;
            foreach (Source audioSource in source.ActiveSources) {
                string id = (0x1000 + ++sourceIndex).ToString("x4"),
                    objectID = "AO_" + id,
                    objectName = "Cavern_Obj_" + sourceIndex,
                    packFormatID = "AP_0003" + id,
                    channelFormatID = "AC_0003" + id,
                    trackID = "ATU_0000" + id,
                    trackFormatID = $"AT_0003{id}_01",
                    streamFormatID = "AS_0003" + id;

                objectIDs.Add(objectID);
                objects.Add(new ADMObject(objectID, "Audio Object " + sourceIndex, default, contentTime, packFormatID) {
                    Tracks = new List<string>() { trackID }
                });
                packFormats.Add(new ADMPackFormat(packFormatID, objectName, ADMPackType.Objects) {
                    ChannelFormats = new List<string>() { channelFormatID }
                });
                channelFormats.Add(new ADMChannelFormat(channelFormatID, objectName, ADMPackType.Objects) {
                    Blocks = movements[sourceIndex - 1]
                });
                tracks.Add(new ADMTrack(trackID, bits, source.SampleRate, trackFormatID, packFormatID));
                trackFormats.Add(new ADMTrackFormat(trackFormatID, "PCM_" + objectName, ADMTrackCodec.PCM, streamFormatID));
                streamFormats.Add(new ADMStreamFormat(streamFormatID, "PCM_" + objectName, ADMTrackCodec.PCM,
                    channelFormatID, packFormatID, trackFormatID));
            }

            adm = new AudioDefinitionModel(programs, contents, objects, packFormats, channelFormats,
                tracks, trackFormats, streamFormats);
        }

        /// <summary>
        /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
        /// </summary>
        public BroadcastWaveFormatWriter(string path, Listener source, long length, BitDepth bits) :
            this(new BinaryWriter(File.OpenWrite(path)), source, length, bits) { }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public override void WriteNextFrame() {
            float[] result = GetInterlacedPCMOutput();
            long writable = output.Length - samplesWritten;
            if (writable > 0) {
                output.WriteBlock(result, 0, Math.Min(Source.UpdateRate, writable) * output.ChannelCount);
            }
            Vector3 scaling = new Vector3(1) / Listener.EnvironmentSize;
            double timeScaling = 1.0 / Source.SampleRate;
            TimeSpan updateTime = TimeSpan.FromSeconds(Source.UpdateRate * timeScaling),
                newOffset = TimeSpan.FromSeconds(samplesWritten * timeScaling);

            int sourceIndex = 0;
            foreach (Source source in Source.ActiveSources) {
                List<ADMBlockFormat> movement = movements[sourceIndex];
                int size = movement.Count;
                Vector3 scaledPosition = source.Position * scaling;
                if (size == 0 || movement[size - 1].Position != scaledPosition) {
                    bool replace = false;
                    if (size > 1) {
                        float t =
                            (float)QMath.LerpInverse(movement[size - 2].Offset, newOffset, movement[size - 1].Offset);
                        Vector3 inBetween = QMath.Lerp(movement[size - 2].Position, scaledPosition, t),
                            diff = inBetween - movement[size - 1].Position;
                        float delta = diff.LengthSquared();
                        if (delta < 0.0001f) {
                            replace = true;
                        }
                    }

                    if (replace) {
                        ADMBlockFormat prev = movement[size - 1];
                        prev.Position = scaledPosition;
                        prev.Duration += updateTime;
                        prev.Interpolation += updateTime;
                    } else {
                        movement.Add(new ADMBlockFormat() {
                            Position = scaledPosition,
                            Offset = newOffset,
                            Duration = updateTime,
                            Interpolation = updateTime
                        });
                    }
                } else {
                    movement[size - 1].Duration += updateTime;
                }
                ++sourceIndex;
            }
            samplesWritten += Source.UpdateRate;
        }

        /// <summary>
        /// Close the writer and export movement metadata.
        /// </summary>
        public override void Dispose() {
            StringBuilder builder = new StringBuilder();
            using (XmlWriter exporter = XmlWriter.Create(builder)) {
                adm.WriteXml(exporter);
            }
            output.WriteChunk(RIFFWave.axmlSync, Encoding.UTF8.GetBytes(builder.ToString().Replace("utf-16", "utf-8")));
            output.Dispose();
        }
    }
}