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
        /// Multimedia container writer base class.
        /// </summary>
        /// <param name="writer">Output stream to write to</param>
        /// <param name="tracks">The source tracks to pack in the container</param>
        protected ContainerWriter(Stream writer, Track[] tracks) {
            this.writer = writer;
            this.tracks = tracks;
        }

        /// <summary>
        /// Multimedia container writer base class.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="tracks">The source tracks to pack in the container</param>
        protected ContainerWriter(string path, Track[] tracks) : this(File.Open(path, FileMode.Create), tracks) { }

        /// <summary>
        /// Write the metadata that is present before the coded content.
        /// </summary>
        public abstract void WriteHeader();

        /// <summary>
        /// Close the writer.
        /// </summary>
        public virtual void Dispose() => writer?.Close();
    }
}