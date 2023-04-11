using System;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Container {
    /// <summary>
    /// Multimedia container writer base class.
    /// </summary>
    public abstract class ContainerWriter : IDisposable {
        /// <summary>
        /// File writer object.
        /// </summary>
        protected Stream writer;

        /// <summary>
        /// The source tracks to pack in the container.
        /// </summary>
        protected Track[] tracks;

        /// <summary>
        /// Length of the played content.
        /// </summary>
        protected double duration;

        /// <summary>
        /// Multimedia container writer base class.
        /// </summary>
        /// <param name="writer">Output stream to write to</param>
        /// <param name="tracks">The source tracks to pack in the container</param>
        /// <param name="duration">Length of the played content</param>
        protected ContainerWriter(Stream writer, Track[] tracks, double duration) {
            this.writer = writer;
            this.tracks = tracks;
            this.duration = duration;
        }

        /// <summary>
        /// Multimedia container writer base class.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="tracks">The source tracks to pack in the container</param>
        /// <param name="duration">Length of the played content</param>
        protected ContainerWriter(string path, Track[] tracks, double duration) :
            this(File.Open(path, FileMode.Create), tracks, duration) { }

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public abstract void WriteHeader();

        /// <summary>
        /// Write the frames that are part of the next block with of a given <see cref="duration"/>.
        /// </summary>
        /// <returns>The writing has finished.</returns>
        public abstract bool WriteBlock(double duration);

        /// <summary>
        /// Close the writer.
        /// </summary>
        public virtual void Dispose() => writer?.Close();
    }
}