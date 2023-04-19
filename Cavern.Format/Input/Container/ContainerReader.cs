using System;
using System.IO;

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
        /// Returns the format of the loaded container.
        /// </summary>
        public abstract Common.Container Type { get; }

        /// <summary>
        /// File reader object.
        /// </summary>
        internal protected Stream reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        protected ContainerReader(Stream reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        protected ContainerReader(string path) => reader = File.OpenRead(path);

        /// <summary>
        /// The following block of the track is rendered and available.
        /// </summary>
        public abstract bool IsNextBlockAvailable(int track);

        /// <summary>
        /// Continue reading a given track.
        /// </summary>
        /// <param name="track">Not the unique <see cref="Track.ID"/>, but its position in the <see cref="Tracks"/> array.</param>
        public abstract byte[] ReadNextBlock(int track);

        /// <summary>
        /// Returns if the next block of a track can be completely decoded by itself.
        /// </summary>
        public virtual bool IsNextBlockKeyframe(int track) => throw new NotImplementedException();

        /// <summary>
        /// Get what is the time offset of the next block in seconds.
        /// </summary>
        /// <returns>Time offset in seconds, or -1 if the last block was passed.</returns>
        public virtual double GetNextBlockOffset(int track) => throw new NotImplementedException();

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