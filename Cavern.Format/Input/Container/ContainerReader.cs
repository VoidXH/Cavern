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
        internal protected Stream reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public ContainerReader(Stream reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public ContainerReader(string path) => reader = File.OpenRead(path);

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
        /// Get the first of the highest available quality audio tracks from the container.
        /// </summary>
        public Track GetMainAudioTrack() {
            Track result = null;
            for (int i = 0; i < Tracks.Length; i++) {
                if (Tracks[i].Format.IsAudio()) {
                    if (result == null || Tracks[i].Format <= result.Format) {
                        result = Tracks[i];
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Close the reader.
        /// </summary>
        public void Dispose() => reader?.Close();
    }
}