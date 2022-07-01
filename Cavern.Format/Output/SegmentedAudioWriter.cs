using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>
    /// Writes audio files with the selected encoder in multiple segments
    /// </summary>
    public class SegmentedAudioWriter : AudioWriter {
        /// <summary>
        /// A C# format string compliant path, where {0} will be the index.
        /// </summary>
        readonly string path;

        /// <summary>
        /// References to all segments.
        /// </summary>
        readonly AudioWriter[] segments;

        /// <summary>
        /// The currently read segment.
        /// </summary>
        int segment;

        /// <summary>
        /// The next sample to read from the current <see cref="segment"/>, for a single channel.
        /// </summary>
        long segmentPosition;

        /// <summary>
        /// Writes audio files with the selected encoder in multiple segments
        /// </summary>
        /// <param name="path">A C# format string compliant path, where {0} will be the index</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="segmentSize">Length of a segment in samples per a single channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public SegmentedAudioWriter(string path, int channelCount, long length, long segmentSize, int sampleRate,
            BitDepth bits) : base((BinaryWriter)null, channelCount, length, sampleRate, bits) {
            this.path = path;
            segments = new AudioWriter[length / segmentSize + (length % segmentSize != 0 ? 1 : 0)];
            long lengthSum = 0;
            for (int i = 0; i < segments.Length; ++i) {
                long lengthHere = Math.Min(segmentSize, length - lengthSum);
                lengthSum += lengthHere;
                segments[i] = Create(string.Format(path, i), channelCount, lengthHere, sampleRate, bits);
            }
        }

        /// <summary>
        /// Get all file names which are loaded by this writer.
        /// </summary>
        public string[] GetSegmentFiles() {
            string[] files = new string[segments.Length];
            for (int i = 0; i < segments.Length; ++i)
                files[i] = string.Format(path, i);
            return files;
        }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() {
            for (int i = 0; i < segments.Length; ++i)
                segments[i].WriteHeader();
        }

        /// <summary>
        /// Write a block of mono or interlaced samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            if (segment == segments.Length)
                return;
            long fromCurrent = (to - from) / ChannelCount,
                remainingInSegment = segments[segment].Length - segmentPosition;
            if (fromCurrent <= remainingInSegment) {
                segments[segment].WriteBlock(samples, from, to);
                segmentPosition += fromCurrent;
            } else {
                segments[segment++].WriteBlock(samples, from, from += remainingInSegment * ChannelCount);
                if (segment != segments.Length)
                    segments[segment].WriteBlock(samples, from, to);
                segmentPosition = fromCurrent - remainingInSegment;
            }
        }

        /// <summary>
        /// Write a block of multichannel samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[][] samples, long from, long to) {
            long fromCurrent = to - from,
                remainingInSegment = segments[segment].Length - segmentPosition;
            if (fromCurrent <= remainingInSegment) {
                segments[segment].WriteBlock(samples, from, to);
                segmentPosition += fromCurrent;
            } else {
                segments[segment++].WriteBlock(samples, from, from + remainingInSegment);
                segments[segment].WriteBlock(samples, from + remainingInSegment, to);
                segmentPosition = fromCurrent - remainingInSegment;
            }
        }

        /// <summary>
        /// Close the files of the segments.
        /// </summary>
        public override void Dispose() {
            for (int i = 0; i < segments.Length; ++i)
                segments[i].Dispose();
        }
    }
}