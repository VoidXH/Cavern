using System;
using System.IO;

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
        /// File writer object.
        /// </summary>
        protected Stream writer;

        /// <summary>
        /// One update of samples to be reused.
        /// </summary>
        readonly float[] renderCache;

        /// <summary>
        /// Exports a listener environment with all its objects, including movement data.
        /// </summary>
        protected EnvironmentWriter(Stream writer, Listener source) {
            Source = source;
            this.writer = writer;
            renderCache = new float[source.UpdateRate * source.ActiveSources.Count];
        }

        /// <summary>
        /// Exports a listener environment with all its objects, including movement data.
        /// </summary>
        protected EnvironmentWriter(string path, Listener source) : this(AudioWriter.Open(path), source) { }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public abstract void WriteNextFrame();

        /// <summary>
        /// Close the writer.
        /// </summary>
        public virtual void Dispose() => writer.Dispose();

        /// <summary>
        /// Gets each source's samples in an interlaced array.
        /// </summary>
        /// <remarks>Only keeps the first streamed channel to each object.</remarks>
        protected float[] GetInterlacedPCMOutput() {
            Source.Ping();
            int channel = 0,
                channels = Source.ActiveSources.Count;
            foreach (Source source in Source.ActiveSources) {
                WaveformUtils.Insert(source.Rendered[0], renderCache, channel++, channels);
            }
            return renderCache;
        }
    }
}