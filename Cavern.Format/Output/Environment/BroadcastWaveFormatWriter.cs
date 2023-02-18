using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Xml;

using Cavern.Format.Consts;
using Cavern.Format.Renderers;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Utilities;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
    /// </summary>
    public class BroadcastWaveFormatWriter : EnvironmentWriter {
        /// <summary>
        /// Reports the progress of the final AXML export [0;1].
        /// </summary>
        public Action<double> FinalFeedback;

        /// <summary>
        /// <see cref="FinalFeedback"/> will report numbers from this to 1.
        /// </summary>
        public double FinalFeedbackStart;

        /// <summary>
        /// The main PCM exporter.
        /// </summary>
        protected readonly RIFFWaveWriter output;

        /// <summary>
        /// Recorded movement path of all sources.
        /// </summary>
        protected readonly List<ADMBlockFormat>[] movements;

        /// <summary>
        /// When not null, writes the AXML to this separate file.
        /// </summary>
        readonly Stream admWriter;

        /// <summary>
        /// Total samples written to the export file.
        /// </summary>
        long samplesWritten;

        /// <summary>
        /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
        /// When an XML path is received, the waveform and the ADM will be written to separate files.
        /// </summary>
        public BroadcastWaveFormatWriter(Stream writer, Listener source, long length, BitDepth bits) :
            base(writer, source) {
            if (writer is FileStream fs && fs.Name.EndsWith(".xml")) {
                admWriter = writer;
                writer = AudioWriter.Open(fs.Name[..^3] + "wav");
            }

            output = new RIFFWaveWriter(writer, source.ActiveSources.Count, length, source.SampleRate, bits) {
                MaxLargeChunks = 1
            };
            output.WriteHeader();

            movements = new List<ADMBlockFormat>[output.ChannelCount];
            for (int i = 0; i < output.ChannelCount; i++) {
                movements[i] = new List<ADMBlockFormat>();
            }
        }

        /// <summary>
        /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
        /// When an XML path is received, the waveform and the ADM will be written to separate files.
        /// </summary>
        public BroadcastWaveFormatWriter(string path, Listener source, long length, BitDepth bits) :
            this(AudioWriter.Open(path), source, length, bits) { }

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
            ADMTimeSpan updateTime = new ADMTimeSpan(Source.UpdateRate * timeScaling),
                newOffset = new ADMTimeSpan(samplesWritten * timeScaling);

            int sourceIndex = 0;
            foreach (Source source in Source.ActiveSources) {
                List<ADMBlockFormat> movement = movements[sourceIndex];
                int size = movement.Count;
                Vector3 scaledPosition = Vector3.Clamp(source.Position * scaling, minusOne, Vector3.One);
                if (size == 0 || movement[size - 1].Position != scaledPosition) {
                    bool replace = false;
                    if (size > 1) {
                        float t = (float)QMath.LerpInverse(movement[size - 2].Offset.TotalSeconds, newOffset.TotalSeconds,
                            movement[size - 1].Offset.TotalSeconds);
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
                        if (size != 0) {
                            movement[size - 1].Duration = newOffset - movement[size - 1].Offset;
                        }
                        movement.Add(new ADMBlockFormat {
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
            AudioDefinitionModel adm = CreateModel();

            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            if (admWriter != null) {
                settings.Indent = true;
            }
            using (XmlWriter exporter = XmlWriter.Create(builder, settings)) {
                adm.WriteXml(exporter);
            }

            byte[] axml = Encoding.UTF8.GetBytes(builder.ToString().Replace("utf-16", "utf-8"));
            if (admWriter == null) {
                output.WriteChunk(RIFFWave.axmlSync, axml, true);
                output.WriteChunk(RIFFWave.chnaSync, ChannelAssignment.GetChunk(adm), true);
                WriteAdditionalChunks();
            } else {
                admWriter.Write(axml);
                admWriter.Dispose();
            }
            output.Dispose();
        }

        /// <summary>
        /// Get the length of the environment recording.
        /// </summary>
        public ADMTimeSpan GetContentLength() => new ADMTimeSpan(output.Length / (double)output.SampleRate);

        /// <summary>
        /// Generates the ADM structure from the recorded movement.
        /// </summary>
        protected virtual AudioDefinitionModel CreateModel() {
            ADMTimeSpan contentLength = GetContentLength();
            const string channelContentID = "ACO_1001",
                objectContentID = "ACO_1002";
            List<string> channelIDs = new List<string>();
            List<string> objectIDs = new List<string>();

            List<ADMProgramme> programs = new List<ADMProgramme> {
                new ADMProgramme("APR_1001", "Cavern_Export", contentLength) {
                    Contents = new List<string> { channelContentID, objectContentID }
                }
            };
            List<ADMContent> contents = new List<ADMContent> {
                new ADMContent(channelContentID, "Channels") {
                    Objects = channelIDs
                },
                new ADMContent(objectContentID, "Objects") {
                    Objects = objectIDs
                }
            };
            List<ADMObject> objects = new List<ADMObject>();
            List<ADMPackFormat> packFormats = new List<ADMPackFormat>();
            List<ADMChannelFormat> channelFormats = new List<ADMChannelFormat>();
            List<ADMTrack> tracks = new List<ADMTrack>();
            List<ADMTrackFormat> trackFormats = new List<ADMTrackFormat>();
            List<ADMStreamFormat> streamFormats = new List<ADMStreamFormat>();

            int channelIndex = 0, objectIndex = 0;
            for (int i = 0; i < movements.Length; ++i) {
                bool isDynamic = movements[i].Count != 1;
                ADMPackType packType = isDynamic ? ADMPackType.Objects : ADMPackType.DirectSpeakers;
                string id = (0x1000 + (isDynamic ? ++objectIndex : ++channelIndex)).ToString("x4"),
                    totalId = (0x1001 + i).ToString("x4"),
                    packHex = ((int)packType).ToString("x4"),
                    objectID = "AO_" + totalId,
                    objectName = isDynamic ? "Cavern_Obj_" + objectIndex :
                        ADMConsts.channelNames[(int)Renderer.ChannelFromPosition(movements[i][0].Position)],
                    packFormatID = $"AP_{packHex}{id}",
                    channelFormatID = $"AC_{packHex}{id}",
                    trackID = "ATU_0000" + (i + 1).ToString("x4"),
                    trackFormatID = $"AT_{packHex}{id}_01",
                    streamFormatID = $"AS_{packHex}{id}";

                if (isDynamic) {
                    objectIDs.Add(objectID);
                } else {
                    channelIDs.Add(objectID);
                }

                objects.Add(new ADMObject(objectID, isDynamic ? "Audio Object " + objectIndex : objectName,
                    default, contentLength, packFormatID) {
                    Tracks = new List<string> { trackID }
                });
                packFormats.Add(new ADMPackFormat(packFormatID, objectName, packType) {
                    ChannelFormats = new List<string> { channelFormatID }
                });

                FixEndTimings(movements[i], contentLength);
                channelFormats.Add(new ADMChannelFormat(channelFormatID, objectName, packType) {
                    Blocks = movements[i]
                });

                tracks.Add(new ADMTrack(trackID, output.Bits, output.SampleRate, trackFormatID, packFormatID));
                trackFormats.Add(new ADMTrackFormat(trackFormatID, "PCM_" + objectName, ADMTrackCodec.PCM, streamFormatID));
                streamFormats.Add(new ADMStreamFormat(streamFormatID, "PCM_" + objectName, ADMTrackCodec.PCM,
                    channelFormatID, packFormatID, trackFormatID));
            }

            contents.RemoveAll(content => content.Objects.Count == 0);

            return new AudioDefinitionModel(programs, contents, objects, packFormats, channelFormats,
                tracks, trackFormats, streamFormats) {
                Feedback = FinalFeedback,
                FeedbackStartPercentage = FinalFeedbackStart
            };
        }

        /// <summary>
        /// Additional chunks to write to the BWF file.
        /// </summary>
        protected virtual void WriteAdditionalChunks() {}

        /// <summary>
        /// Makes sure the last block ends with the content.
        /// </summary>
        protected void FixEndTimings(List<ADMBlockFormat> blocks, ADMTimeSpan contentLength) {
            for (int j = 0, c = blocks.Count; j < c; j++) {
                if (blocks.Count != 0) {
                    ADMBlockFormat lastBlock = blocks[^1];
                    if (!lastBlock.Duration.IsZero() && !contentLength.Equals(lastBlock.Offset + lastBlock.Duration)) {
                        ADMTimeSpan newDuration = contentLength - lastBlock.Offset;
                        if (lastBlock.Interpolation.Equals(lastBlock.Duration)) {
                            lastBlock.Interpolation = newDuration;
                        }
                        lastBlock.Duration = newDuration;
                    }
                    ADMBlockFormat fistBlock = blocks[0];
                    if (!fistBlock.Interpolation.IsZero()) {
                        fistBlock.Interpolation = ADMTimeSpan.Zero;
                    }
                }
            }
        }

        /// <summary>
        /// Inverse of <see cref="Vector3.One"/> for clamping.
        /// </summary>
        static readonly Vector3 minusOne = -Vector3.One;
    }
}