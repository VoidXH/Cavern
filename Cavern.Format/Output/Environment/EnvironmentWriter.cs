using System;
using System.IO;
using System.Linq;

using Cavern.Format.Common;
using Cavern.Format.Environment.Utilities;
using Cavern.Format.Exceptions;
using Cavern.Format.Renderers;
using Cavern.Utilities;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Exports a listener environment with all its sources, including movement data.
    /// </summary>
    public abstract class EnvironmentWriter : IDisposable {
        /// <summary>
        /// The rendering environment to export.
        /// </summary>
        public Listener Source { get; protected set; }

        /// <summary>
        /// Content length in samples.
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// File writer object.
        /// </summary>
        protected Stream writer;

        /// <summary>
        /// Binary representation of samples.
        /// </summary>
        protected readonly BitDepth bits;

        /// <summary>
        /// One update of samples to be reused.
        /// </summary>
        float[] renderCache;

        /// <summary>
        /// Exports a listener environment with all its objects, including movement data.
        /// </summary>
        protected EnvironmentWriter(Stream writer, Listener source, long length, BitDepth bits) {
            Source = source;
            Length = length;
            this.writer = writer;
            this.bits = bits;
        }

        /// <summary>
        /// Exports a listener environment with all its objects, including movement data.
        /// </summary>
        protected EnvironmentWriter(string path, Listener source, long length, BitDepth bits) :
            this(AudioWriter.Open(path), source, length, bits) { }

        /// <summary>
        /// Create the <see cref="EnvironmentWriter"/> for the specified <paramref name="codec"/>.
        /// </summary>
        /// <param name="path">Create the output file with this name</param>
        /// <param name="codec">Used audio format</param>
        /// <param name="environment">The <see cref="Cavern.Source"/> samples and movements will be taken from this rendering environment</param>
        /// <param name="length">Total sample count in the created file</param>
        /// <param name="bits">Output file bit depth</param>
        public static EnvironmentWriter Create(string path, Codec codec, Listener environment, long length, BitDepth bits) =>
            Create(path, codec, environment, length, bits, null);

        /// <summary>
        /// Create the <see cref="EnvironmentWriter"/> for the specified <paramref name="codec"/>.
        /// </summary>
        /// <param name="path">Create the output file with this name</param>
        /// <param name="codec">Used audio format</param>
        /// <param name="environment">The <see cref="Cavern.Source"/> samples and movements will be taken from this rendering environment</param>
        /// <param name="length">Total sample count in the created file</param>
        /// <param name="bits">Output file bit depth</param>
        /// <param name="renderer">When the source is a decoded audio stream, take its static objects - this can be null</param>
        public static EnvironmentWriter Create(string path, Codec codec, Listener environment, long length, BitDepth bits, Renderer renderer) {
            return codec switch {
                Codec.LimitlessAudio => new LimitlessAudioFormatEnvironmentWriter(path, environment, length, bits),
                Codec.ADM_BWF => new BroadcastWaveFormatWriter(path, environment, length, bits),
                Codec.ADM_BWF_Atmos => new DolbyAtmosBWFWriter(path, environment, length, bits, renderer, false),
                Codec.DAMF => new DolbyAtmosMasterFormatWriter(path, environment, length, bits, renderer),
                _ => throw new UnsupportedContainerForWriteException(codec.ToString()),
            };
        }

        /// <summary>
        /// Create the <see cref="EnvironmentWriter"/> for the specified <paramref name="codec"/>.
        /// </summary>
        /// <param name="stream">Write to this stream</param>
        /// <param name="codec">Used audio format</param>
        /// <param name="environment">The <see cref="Cavern.Source"/> samples and movements will be taken from this rendering environment</param>
        /// <param name="length">Total sample count in the created file</param>
        /// <param name="bits">Output file bit depth</param>
        public static EnvironmentWriter Create(Stream stream, Codec codec, Listener environment, long length, BitDepth bits) =>
            Create(stream, codec, environment, length, bits, null);

        /// <summary>
        /// Create the <see cref="EnvironmentWriter"/> for the specified <paramref name="codec"/>.
        /// </summary>
        /// <param name="stream">Write to this stream</param>
        /// <param name="codec">Used audio format</param>
        /// <param name="environment">The <see cref="Cavern.Source"/> samples and movements will be taken from this rendering environment</param>
        /// <param name="length">Total sample count in the created file</param>
        /// <param name="bits">Output file bit depth</param>
        /// <param name="renderer">When the source is a decoded audio stream, take its static objects - this can be null</param>
        public static EnvironmentWriter Create(Stream stream, Codec codec, Listener environment, long length, BitDepth bits, Renderer renderer) {
            return codec switch {
                Codec.LimitlessAudio => new LimitlessAudioFormatEnvironmentWriter(stream, environment, length, bits),
                Codec.ADM_BWF => new BroadcastWaveFormatWriter(stream, environment, length, bits),
                Codec.ADM_BWF_Atmos => new DolbyAtmosBWFWriter(stream, environment, length, bits, renderer, false),
                Codec.DAMF => throw new UnsupportedFeatureException("Stream-based DAMF"),
                _ => throw new UnsupportedContainerForWriteException(codec.ToString()),
            };
        }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public abstract void WriteNextFrame();

        /// <inheritdoc/>
        public virtual void Dispose() => writer.Dispose();

        /// <summary>
        /// Perform corrections for cases that might break <paramref name="environment"/>al exports for codecs with initially defined <paramref name="staticSources"/>.
        /// This shall be called after the first frame is rendered, but before it is written to the output file, becuase a common case is the static sources being removed.
        /// </summary>
        protected void StaticSourceCorrection(Listener environment, ref StaticSource[] staticSources) {
            bool[] existenceMap = new bool[staticSources.Length];
            foreach (Source source in environment.ActiveSources) {
                for (int i = 0; i < staticSources.Length; i++) {
                    if (staticSources[i].Source == source) {
                        existenceMap[i] = true;
                        break;
                    }
                }
            }
            staticSources = staticSources.Where((_, i) => existenceMap[i]).ToArray();
        }

        /// <summary>
        /// Gets each source's samples in an interlaced array.
        /// </summary>
        /// <remarks>Only keeps the first streamed channel to each object.</remarks>
        protected float[] GetInterlacedPCMOutput() {
            Source.Ping();
            renderCache ??= new float[Source.UpdateRate * Source.ActiveSources.Count];
            int channel = 0,
                channels = Source.ActiveSources.Count;
            foreach (Source source in Source.ActiveSources) {
                WaveformUtils.Insert(source.Rendered[0], renderCache, channel++, channels);
            }
            return renderCache;
        }
    }
}
