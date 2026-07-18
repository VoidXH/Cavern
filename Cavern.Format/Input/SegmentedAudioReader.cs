using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Reads audio files from multiple segments.
    /// </summary>
    public class SegmentedAudioReader : AudioReader {
        /// <inheritdoc/>
        public override long Position {
            get => -1;
            set {
                long start = 0;
                for (int i = 0, c = segments.Count; i < c; i++) {
                    if (start > value) {
                        segments[i].Position = 0;
                    }
                    if (start + segments[i].Length > value) {
                        segment = i;
                        segmentPosition = value - start;
                        segments[i].Position = segmentPosition;
                    }
                }
            }
        }

        /// <summary>
        /// A C# format string compliant path, where {0} will be the index.
        /// </summary>
        readonly string path;

        /// <summary>
        /// References to all segments.
        /// </summary>
        readonly List<AudioReader> segments = new List<AudioReader>();

        /// <summary>
        /// The currently read segment.
        /// </summary>
        int segment;

        /// <summary>
        /// The next sample to read from the current <see cref="segment"/>, for a single channel.
        /// </summary>
        long segmentPosition;

        /// <summary>
        /// Reads audio files from multiple segments.
        /// </summary>
        /// <param name="path">A C# format string compliant path, where {0} will be the index</param>
        public SegmentedAudioReader(string path) : base((Stream)null) {
            this.path = path;
            while (true) {
                string segmentPath = string.Format(path, segments.Count);
                if (!File.Exists(segmentPath)) {
                    break;
                }
                segments.Add(Open(segmentPath));
            }
            if (segments.Count == 0) {
                throw new FileNotFoundException(path);
            }

            segments[0].ReadHeader();
            ChannelCount = segments[0].ChannelCount;
            Length = segments.Sum(x => x.Length);
            SampleRate = segments[0].SampleRate;
            Bits = segments[0].Bits;
        }

        /// <summary>
        /// Get all file names which are loaded by this reader.
        /// </summary>
        public string[] GetSegmentFiles() {
            string[] files = new string[segments.Count];
            for (int i = 0, c = segments.Count; i < c; ++i) {
                files[i] = string.Format(path, i);
            }
            return files;
        }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            for (int i = 0, c = segments.Count; i < c; ++i) {
                segments[i].Reset();
            }
        }

        /// <inheritdoc/>
        public override void ReadBlock(float[] samples, long from, long to) {
            if (segment == segments.Count) {
                return;
            }
            long fromCurrent = (to - from) / ChannelCount,
                remainingInSegment = segments[segment].Length - segmentPosition;
            if (fromCurrent <= remainingInSegment) {
                segments[segment].ReadBlock(samples, from, to);
                segmentPosition += fromCurrent;
            } else {
                segments[segment++].ReadBlock(samples, from, from += remainingInSegment * ChannelCount);
                if (segment != segments.Count) {
                    segments[segment].ReadBlock(samples, from, to);
                }
                segmentPosition = fromCurrent - remainingInSegment;
            }
        }

        /// <inheritdoc/>
        public override void Reset() {
            segment = 0;
            segmentPosition = 0;
            for (int i = 0, c = segments.Count; i < c; ++i) {
                segments[i].Reset();
            }
        }

        /// <inheritdoc/>
        public override Renderer GetRenderer() => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public override void Dispose() {
            for (int i = 0, c = segments.Count; i < c; ++i) {
                segments[i].Dispose();
            }
        }
    }
}
