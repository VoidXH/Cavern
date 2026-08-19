using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

using Cavern.Channels;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Exceptions;
using Cavern.Format.Renderers;
using Cavern.Format.Transcoders;
using Cavern.Format.Utilities;
using Cavern.Utilities;
using Cavern.Waveforms.Utilities;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
    /// </summary>
    public class DolbyAtmosMasterFormatWriter : EnvironmentWriter {
        /// <summary>
        /// All active <see cref="Source"/>s in the <see cref="Listener"/>.
        /// </summary>
        Source[] sources;

        /// <summary>
        /// At constructor time, these original <see cref="Source"/>s held static bed channels.
        /// </summary>
        StaticSource[] staticSources;

        /// <summary>
        /// IDs used in the metadata file for each PCM track.
        /// </summary>
        int[] channelIDs;

        /// <summary>
        /// The last updates of each <see cref="Source"/>.
        /// </summary>
        MovementTimeframe[] lastFrames;

        /// <summary>
        /// Transform <see cref="Source"/> movements to codec scale.
        /// </summary>
        Vector3 scaling;

        /// <summary>
        /// PCM samples are written to this file.
        /// </summary>
        CoreAudioFormatWriter pcmOut;

        /// <summary>
        /// Makes sure the static channels prepend the objects.
        /// </summary>
        InterlacedWaveformReorderer reorderer;

        /// <summary>
        /// YAML metadata is written to this file.
        /// </summary>
        StreamWriter metadataOut;

        /// <summary>
        /// Total samples written to the export file.
        /// </summary>
        long samplesWritten;

        /// <summary>
        /// Last sample position when positions were updated.
        /// </summary>
        long lastUpdate;

        /// <summary>
        /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
        /// </summary>
        public DolbyAtmosMasterFormatWriter(Stream writer, Listener source, long length, BitDepth bits, params StaticSource[] staticSources) :
            base(writer, source, length, bits) => this.staticSources = staticSources;

        /// <summary>
        /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
        /// </summary>
        public DolbyAtmosMasterFormatWriter(string path, Listener source, long length, BitDepth bits, params StaticSource[] staticSources) :
            this(AudioWriter.Open(path), source, length, bits, staticSources) { }

        /// <summary>
        /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
        /// </summary>
        public DolbyAtmosMasterFormatWriter(string path, Listener source, long length, BitDepth bits, Renderer renderer) :
            this(path, source, length, bits, ParseStaticSources(StaticSourceHandler.GetStaticSources(source, renderer))) { }

        /// <summary>
        /// Match a set of <see cref="StaticSource"/>s to the channel layout which some tools require.
        /// </summary>
        static StaticSource[] ParseStaticSources(StaticSource[] input) {
            // Handle the RL RR SL SR ending, which not all tools can handle, but they can handle SL SR RL RR
            if (input.Length >= 8 && input[4].Channel == ReferenceChannel.RearLeft && input[5].Channel == ReferenceChannel.RearRight &&
                input[6].Channel == ReferenceChannel.SideLeft && input[7].Channel == ReferenceChannel.SideRight) {
                (input[4], input[6]) = (input[6], input[4]);
                (input[5], input[7]) = (input[7], input[5]);
            }
            return input;
        }

        /// <inheritdoc/>
        public override void WriteNextFrame() {
            float[] result = GetInterlacedPCMOutput(); // Render the first frame before outputting the Sources, since filters like Cavernize can make more
            if (pcmOut == null) {
                Source[] sources = Source.ActiveSources.ToArray();
                int[] mapping = new int[sources.Length];
                for (int i = 0; i < mapping.Length; i++) {
                    mapping[i] = i;
                }

                for (int i = 0; i < sources.Length; i++) {
                    int target = staticSources.IndexOf(x => x.Source == sources[i]);
                    if (target == -1) {
                        continue;
                    }

                    int currentMappedTo = Array.IndexOf(mapping, target);
                    (mapping[i], mapping[currentMappedTo]) = (mapping[currentMappedTo], mapping[i]);
                }

                reorderer = new InterlacedWaveformReorderer(mapping);
                StaticSourceCorrection(Source, ref staticSources);
                CreateFiles();
            }

            long writable = pcmOut.Length - samplesWritten;
            if (writable > 0) {
                reorderer.Process(result);
                pcmOut.WriteBlock(result, 0, Math.Min(Source.UpdateRate, writable) * pcmOut.ChannelCount);
            }

            for (int i = 0; i < sources.Length; i++) {
                Vector3 scaledPos = sources[i].Position * scaling;
                float gain = QMath.GainToDb(sources[i].Volume);
                bool positionChanged = scaledPos != lastFrames[i].position;
                bool gainChanged = gain != lastFrames[i].gain;
                if (!positionChanged && !gainChanged) {
                    continue;
                }

                metadataOut.WriteLine("  - ID: " + channelIDs[i]);
                if (lastUpdate != samplesWritten) {
                    metadataOut.WriteLine("    samplePos: " + samplesWritten);
                    lastUpdate = samplesWritten;
                }
                if (positionChanged) {
                    WriteMetadataPosition(scaledPos);
                }
                if (gainChanged) {
                    WriteMetadataGain(gain);
                }
                lastFrames[i] = new MovementTimeframe(scaledPos, gain, 0, 0);
            }

            samplesWritten += Source.UpdateRate;
        }

        /// <inheritdoc/>
        public override void Dispose() {
            base.Dispose();
            pcmOut?.Dispose();
            metadataOut?.Dispose();
        }

        /// <summary>
        /// Create the metadata and audio files.
        /// </summary>
        void CreateFiles() {
            if (!(writer is FileStream fileStream)) {
                throw new StreamingNotSupportedException();
            }

            sources = Source.ActiveSources.ToArray();
            channelIDs = new int[sources.Length];
            lastFrames = new MovementTimeframe[sources.Length];
            scaling = new Vector3(1) / Listener.EnvironmentSize;

            DolbyAtmosMasterFormatRootFile rootFile = new DolbyAtmosMasterFormatRootFile(staticSources, sources, channelIDs);
            rootFile.Export(writer);
            int bedChannels = rootFile.BedChannelCount;

            pcmOut = new CoreAudioFormatWriter(fileStream.Name + ".audio", sources.Length, Length, Source.SampleRate, bits);
            pcmOut.WriteHeader();

            metadataOut = new StreamWriter(fileStream.Name + ".metadata");
            metadataOut.WriteLine("sampleRate: " + Source.SampleRate);
            metadataOut.WriteLine("events:");
            for (int i = 0; i < sources.Length; i++) {
                Vector3 scaledPos = sources[i].Position * scaling;
                float gain = QMath.GainToDb(sources[i].Volume);
                metadataOut.WriteLine("  - ID: " + channelIDs[i]);
                metadataOut.WriteLine("    samplePos: 0");
                metadataOut.WriteLine("    active: true");
                if (i >= bedChannels) {
                    WriteMetadataPosition(scaledPos);
                    metadataOut.WriteLine("    snap: false");
                    metadataOut.WriteLine("    elevation: true");
                    metadataOut.WriteLine("    zones: all");
                    metadataOut.WriteLine("    size: " + sources[i].Size);
                    metadataOut.WriteLine("    decorr: 0");
                }
                metadataOut.WriteLine("    importance: 1");
                WriteMetadataGain(gain);
                metadataOut.WriteLine("    rampLength: 0");
                metadataOut.WriteLine("    trimBypass: false");
                if (i >= bedChannels) {
                    metadataOut.WriteLine("    dialog: -1");
                    metadataOut.WriteLine("    music: -1");
                    metadataOut.WriteLine("    screenFactor: 0");
                    metadataOut.WriteLine("    depthFactor: 0.25");
                    metadataOut.WriteLine("    rampLength: " + Source.UpdateRate);
                }
                metadataOut.WriteLine("    headTrackMode: undefined");
                metadataOut.Write("    binauralRenderMode: ");
                metadataOut.WriteLine(i < bedChannels ? "off" : "undefined");
            }
        }

        /// <summary>
        /// Write the selected <see cref="Source"/>'s <paramref name="position"/> to the metadata file.
        /// </summary>
        void WriteMetadataPosition(Vector3 position) {
            string x = position.X.ToString(CultureInfo.InvariantCulture);
            string y = position.Y.ToString(CultureInfo.InvariantCulture);
            string z = position.Z.ToString(CultureInfo.InvariantCulture);
            metadataOut.WriteLine($"    pos: [{x}, {z}, {y}]");
        }

        /// <summary>
        /// Write the selected <see cref="Source"/>'s <paramref name="gain"/> (given in dB) to the metadata file.
        /// </summary>
        void WriteMetadataGain(float gain) =>
            metadataOut.WriteLine("    gain: " + gain.ToString(CultureInfo.InvariantCulture));
    }
}
