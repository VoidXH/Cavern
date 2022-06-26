using System;
using System.IO;
using System.Linq;
using Cavern.Format.Common;

namespace Cavern.Format.Container {
    /// <summary>
    /// Multimedia container reader base class.
    /// </summary>
    public abstract class ContainerReader : IDisposable {
        /// <summary>
        /// Content length in seconds.
        /// </summary>
        public double Duration { get; protected set; }

        /// <summary>
        /// Metadata of media streams in this file.
        /// </summary>
        public Track[] Tracks { get; protected set; }

        /// <summary>
        /// File reader object.
        /// </summary>
        internal protected BinaryReader reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public ContainerReader(BinaryReader reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public ContainerReader(string path) => reader = new BinaryReader(File.OpenRead(path));

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the <see cref="Tracks"/> array.</param>
        public abstract byte[] ReadNextBlock(int track);

        /// <summary>
        /// Start the following reads from the selected timestamp.
        /// </summary>
        /// <returns>Position that was actually possible to seek to or -1 if the position didn't change.</returns>
        public abstract double Seek(double position);

        /// <summary>
        /// Get the main audio track from the container.
        /// </summary>
        public Track GetFirstAudioTrack() => Tracks.FirstOrDefault(x => x.Format.IsAudio());

        /// <summary>
        /// Close the reader.
        /// </summary>
        public void Dispose() => reader?.Close();
    }
}