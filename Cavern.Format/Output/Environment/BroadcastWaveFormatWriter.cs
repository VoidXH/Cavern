using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;

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
        // TODO: don't allow writing over wav tag limits, check last frame size
        long samplesWritten;

        /// <summary>
        /// Object-based exporter of a listening environment to Audio Definition Model Broadcast Wave Format.
        /// </summary>
        public BroadcastWaveFormatWriter(BinaryWriter writer, Listener source, long length, BitDepth bits) :
            base(writer, source) {
            output = new RIFFWaveWriter(writer, source.ActiveSources.Count, length, source.SampleRate, bits);
            output.WriteHeader();

            movements = new List<ADMBlockFormat>[output.ChannelCount];
            for (int i = 0; i < output.ChannelCount; i++) {
                movements[i] = new List<ADMBlockFormat>();
            }

            double contentLength = length / (double)source.SampleRate;
            List<ADMObject> objects = new List<ADMObject>();
            int sourceIndex = 0;
            foreach (Source audioSource in source.ActiveSources) {
                string id = (0x1000 + ++sourceIndex).ToString("X4");
                objects.Add(new ADMObject("AO_" + id, "Audio Object " + sourceIndex, 0, contentLength) {
                    PackFormat = new ADMPackFormat() {
                        ID = "AP_0003" + id,
                        Name = "Cavern_Obj_" + sourceIndex,
                        Type = ADMPackType.Objects,
                        ChannelFormats = new List<ADMChannelFormat> {
                            new ADMChannelFormat() {
                                ID = "AC_0003" + id,
                                Name = "Cavern_Obj_" + sourceIndex,
                                Blocks = movements[sourceIndex - 1]
                            }
                        }
                    }
                });
            }

            adm = new AudioDefinitionModel(new List<ADMProgramme> {
                new ADMProgramme("APR_1001", "Cavern_Export", contentLength) {
                    Contents = new List<ADMContent> {
                        new ADMContent() {
                            ID = "ACO_1001",
                            Name = "Objects",
                            Objects = objects
                        }
                    }
                }
            }, source.SampleRate);
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
            output.WriteBlock(result, 0, result.Length);
            int sourceIndex = 0;
            foreach (Source source in Source.ActiveSources) {
                int size = movements[sourceIndex].Count;
                if (size == 0 || movements[sourceIndex][size - 1].Position != source.Position) {
                    movements[sourceIndex].Add(new ADMBlockFormat() {
                        Position = source.Position,
                        Offset = samplesWritten,
                        Duration = Source.UpdateRate,
                        Interpolation = Source.UpdateRate
                    });
                } else {
                    movements[sourceIndex][size - 1].Duration += Source.UpdateRate;
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
            using XmlWriter exporter = XmlWriter.Create(builder);
            adm.WriteXml(exporter);
            output.WriteChunk(RIFFWave.axmlSync, Encoding.UTF8.GetBytes(builder.ToString()));
            output.Dispose();
        }
    }
}