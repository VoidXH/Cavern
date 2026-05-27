using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
    /// </summary>
    public class DolbyAtmosMasterFormatWriter : EnvironmentWriter {
        /// <summary>
        /// At constructor time, these original <see cref="Source"/>s held static bed channels.
        /// </summary>
        readonly (ReferenceChannel, Source)[] staticObjects;

        /// <summary>
        /// All active <see cref="Source"/>s in the <see cref="Listener"/>.
        /// </summary>
        Source[] sources;

        /// <summary>
        /// Number of <see cref="Source"/>s that are marked as static channels.
        /// </summary>
        int bedChannels;

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
        /// Local render cache matching this writer's sources ordering.
        /// </summary>
        float[] orderedRenderCache;

        /// <summary>
        /// PCM samples are written to this file.
        /// </summary>
        CoreAudioFormatWriter pcmOut;

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
        public DolbyAtmosMasterFormatWriter(Stream writer, Listener source, long length, BitDepth bits,
            params (ReferenceChannel, Source)[] staticObjects) :
            base(writer, source, length, bits) => this.staticObjects = staticObjects;

        /// <summary>
        /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
        /// </summary>
        public DolbyAtmosMasterFormatWriter(string path, Listener source, long length, BitDepth bits,
            params (ReferenceChannel, Source)[] staticObjects) :
            this(AudioWriter.Open(path), source, length, bits, staticObjects) { }

        /// <summary>
        /// Object-based exporter of a listening environment to Dolby Atmos Master Format.
        /// </summary>
        public DolbyAtmosMasterFormatWriter(string path, Listener source, long length, BitDepth bits, Renderer renderer) :
            this(path, source, length, bits, StaticSourceHandler.GetStaticObjects(renderer)) { }

        /// <summary>
        /// Translate <see cref="ReferenceChannel"/>s to Dolby Atmos bed channel IDs.
        /// </summary>
        static int GetBedChannelID(ReferenceChannel channel) {
            if (channel <= ReferenceChannel.SideRight) {
                return (int)channel;
            } else if (channel == ReferenceChannel.TopSideLeft) {
                return 8;
            } else if (channel == ReferenceChannel.TopSideRight) {
                return 9;
            } else {
                if (CavernFormatGlobal.Unsafe) {
                    return -1;
                } else {
                    throw new InvalidChannelException(channel);
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteNextFrame() {
            if (pcmOut == null) {
                CreateFiles();
            }

            // Use the local sources ordering (beds first) to build interlaced PCM so it matches metadata IDs
            float[] result = GetInterlacedPCMOutputOrdered();
            long writable = pcmOut.Length - samplesWritten;
            if (writable > 0) {
                pcmOut.WriteBlock(result, 0, Math.Min(Source.UpdateRate, writable) * pcmOut.ChannelCount);
            }

        // Build interlaced PCM output using this writer's sources ordering so metadata IDs match PCM channels.
        float[] GetInterlacedPCMOutputOrdered() {
            Source.Ping();
            orderedRenderCache ??= new float[Source.UpdateRate * sources.Length];
            int channel = 0,
                channels = sources.Length;
            foreach (Source source in sources) {
                WaveformUtils.Insert(source.Rendered[0], orderedRenderCache, channel++, channels);
            }
            return orderedRenderCache;
        }

            if (pcmOut != null) { // Do not update in the first frame, it happens in CreateFiles
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

            // Treat only the ScreenLFE (FEL / 低音) static objects as bed channels.
            var bedStatics = staticObjects
                .Where(x => x.Item1 == ReferenceChannel.ScreenLFE)
                .ToArray();
            int[] bedIDs = bedStatics
                .Select(x => GetBedChannelID(x.Item1))
                .TakeWhile(x => x != -1)
                .ToArray();
            int sourceCount = Source.ActiveSources.Count;

            var active = Source.ActiveSources.ToArray();
            // Order sources so that bed sources (ScreenLFE) come first, preserving bedStatics order
            var ordered = new System.Collections.Generic.List<Source>(sourceCount);
            foreach (var b in bedStatics) {
                ordered.Add(b.Item2);
            }
            foreach (var s in active) {
                if (!bedStatics.Any(b => b.Item2 == s)) ordered.Add(s);
            }
            sources = ordered.ToArray();
            channelIDs = new int[sourceCount];
            lastFrames = new MovementTimeframe[sourceCount];
            scaling = new Vector3(1) / Listener.EnvironmentSize;

            // Assign IDs: first bed IDs, then object IDs sequentially starting at 10
            bedChannels = bedIDs.Length;
            for (int i = 0; i < bedChannels; i++) channelIDs[i] = bedIDs[i];
            for (int i = bedChannels; i < sourceCount; i++) channelIDs[i] = 10 + (i - bedChannels);
            var objectIDs = new System.Collections.Generic.List<int>();
            for (int i = bedChannels; i < sourceCount; i++) objectIDs.Add(channelIDs[i]);

            using StreamWriter root = new StreamWriter(writer);
            string rootFile = Path.GetFileName(((FileStream)writer).Name);

            root.WriteLine("version: 0.5.1");
            root.WriteLine("presentations:");
            root.WriteLine("  - type: home");
            root.WriteLine("    simplified: false");
            root.WriteLine($"    metadata: {rootFile}.metadata");
            root.WriteLine($"    audio: {rootFile}.audio");
            root.WriteLine("    offset: 0.0");
            root.WriteLine("    fps: 24");
            root.WriteLine($"    scBedConfiguration: [{string.Join(", ", bedIDs)}]");
            root.WriteLine("    creationTool: Cavern");
            root.WriteLine("    creationToolVersion: " + Listener.Version);
            root.WriteLine("    bedInstances:");

            if (bedChannels == 0) {
                root.WriteLine("      - channels: []");
            } else {
                root.WriteLine("      - channels:");
                for (int i = 0; i < bedChannels; i++) {
                    root.WriteLine("          - channel: " + bedStatics[i].Item1.GetShortName());
                    root.WriteLine("            ID: " + bedIDs[i]);
                }
            }

            if (objectIDs.Count == 0) {
                root.WriteLine("    objects: []");
            } else {
                root.WriteLine("    objects:");
                foreach (int id in objectIDs) {
                    root.WriteLine("      - ID: " + id);
                }
            }

            pcmOut = new CoreAudioFormatWriter(fileStream.Name + ".audio", sources.Length, length, Source.SampleRate, bits);
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
                bool isBed = System.Array.IndexOf(bedIDs, channelIDs[i]) >= 0;
                if (!isBed) {
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
                if (!isBed) {
                    metadataOut.WriteLine("    dialog: -1");
                    metadataOut.WriteLine("    music: -1");
                    metadataOut.WriteLine("    screenFactor: 0");
                    metadataOut.WriteLine("    depthFactor: 0.25");
                    metadataOut.WriteLine("    rampLength: " + Source.UpdateRate);
                }
                metadataOut.WriteLine("    headTrackMode: undefined");
                metadataOut.Write("    binauralRenderMode: ");
                metadataOut.WriteLine(isBed ? "off" : "undefined");
                lastFrames[i] = new MovementTimeframe(scaledPos, gain, 0, 0);
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
