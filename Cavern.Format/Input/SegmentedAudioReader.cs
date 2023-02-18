using Cavern.Format.Renderers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cavern.Format {
    /// <summary>
    /// Reads audio files from multiple segments.
    /// </summary>
    public class SegmentedAudioReader : AudioReader {
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

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
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

        /// <summary>
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public override void Reset() {
            segment = 0;
            segmentPosition = 0;
            for (int i = 0, c = segments.Count; i < c; ++i) {
                segments[i].Reset();
            }
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public override void Seek(long sample) {
            long start = 0;
            for (int i = 0, c = segments.Count; i < c; ++i) {
                if (start > sample) {
                    segments[i].Seek(0);
                }
                if (start + segments[i].Length > sample) {
                    segment = i;
                    segmentPosition = sample - start;
                    segments[i].Seek(segmentPosition);
                }
            }
        }

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() => throw new System.NotImplementedException();

        /// <summary>
        /// Close the files of the segments.
        /// </summary>
        public override void Dispose() {
            for (int i = 0, c = segments.Count; i < c; ++i) {
                segments[i].Dispose();
            }
        }
    }
}