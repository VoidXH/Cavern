using System;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format.Operations {
    /// <summary>
    /// Extracts a single track from a container format, such as an MKV file.
    /// </summary>
    public sealed class ExtractTrackFromContainer : IDisposable {
        /// <summary>
        /// The track to extract from the container.
        /// </summary>
        readonly Track track;

        /// <summary>
        /// Where to write the extracted track.
        /// </summary>
        readonly Stream output;

        /// <summary>
        /// Extracts a single track from a container format, such as an MKV file.
        /// </summary>
        public ExtractTrackFromContainer(Track track, Stream output) {
            this.track = track;
            this.output = output;
        }

        /// <summary>
        /// Write the next data chunk to the output.
        /// </summary>
        /// <returns>True if the extraction is still in progress - so false on completion.</returns>
        public bool Process() {
            if (!track.IsNextBlockAvailable()) {
                return false;
            }

            byte[] data = track.ReadNextBlock();
            output.Write(data);
            return true;
        }

        /// <inheritdoc/>
        public void Dispose() => output.Dispose();
    }
}
