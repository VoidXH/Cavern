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
        /// Length of a segment in samples per a single channel, excluding the overlap.
        /// </summary>
        readonly long segmentSize;

        /// <summary>
        /// References to all segments.
        /// </summary>
        readonly AudioWriter[] segments;

        /// <summary>
        /// Total number of samples written across all segments (for a single channel), not counting overlaps.
        /// </summary>
        long written;

        /// <summary>
        /// Writes audio files with the selected encoder in multiple segments.
        /// </summary>
        /// <param name="path">A C# format string compliant path, where {0} will be the index</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="segmentSize">Length of a segment in samples per a single channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public SegmentedAudioWriter(string path, int channelCount, long length, long segmentSize, int sampleRate,
            BitDepth bits) : this(path, channelCount, length, segmentSize, sampleRate, bits, 0) { }

        /// <summary>
        /// Writes audio files with the selected encoder in multiple overlapping segments.
        /// </summary>
        /// <param name="path">A C# format string compliant path, where {0} will be the index</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="segmentSize">Length of a segment in samples per a single channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="overlap">Number of added samples of a segment to also contain in the next segment.</param>
        public SegmentedAudioWriter(string path, int channelCount, long length, long segmentSize, int sampleRate,
            BitDepth bits, long overlap) : base((Stream)null, channelCount, length, sampleRate, bits) {
            this.path = path;
            this.segmentSize = segmentSize;
            segments = new AudioWriter[length / segmentSize + (length % segmentSize != 0 ? 1 : 0)];
            long lengthSum = 0;
            for (int i = 0; i < segments.Length; i++) {
                long lengthHere = Math.Min(segmentSize, length - lengthSum);
                lengthSum += lengthHere;
                segments[i] = Create(string.Format(path, i), channelCount,
                    lengthHere + Math.Min(overlap, length - lengthSum), sampleRate, bits);
            }
        }

        /// <summary>
        /// Get all file names which are loaded by this writer.
        /// </summary>
        public string[] GetSegmentFiles() {
            string[] files = new string[segments.Length];
            for (int i = 0; i < segments.Length; i++) {
                files[i] = string.Format(path, i);
            }
            return files;
        }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() {
            for (int i = 0; i < segments.Length; i++) {
                segments[i].WriteHeader();
            }
        }

        /// <summary>
        /// Write a block of mono or interlaced samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            long blockLength = (to - from) / ChannelCount;
            for (int i = Math.Max((int)(written / segmentSize) - 1, 0); i < segments.Length; i++) {
                long segmentStart = i * segmentSize,
                    segmentOverlap = segmentStart + segments[i].Length;
                if (segmentStart <= written && segmentOverlap > written) {
                    long offset = written - segmentStart;
                    long samplesToWrite = Math.Min(segmentOverlap - offset, blockLength) * ChannelCount;
                    segments[i].WriteBlock(samples, from, from + samplesToWrite);
                } else {
                    long blockEnd = written + blockLength;
                    if (segmentStart < blockEnd && segmentOverlap > blockEnd) {
                        long samplesToWrite = Math.Min(blockEnd - segmentStart, blockLength) * ChannelCount;
                        segments[i].WriteBlock(samples, to - samplesToWrite, samplesToWrite);
                        break;
                    }
                }
            }
            written += blockLength;
        }

        /// <summary>
        /// Write a block of multichannel samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[][] samples, long from, long to) {
            long blockLength = to - from;
            for (int i = Math.Max((int)(written / segmentSize) - 1, 0); i < segments.Length; i++) {
                long segmentStart = i * segmentSize,
                    segmentOverlap = segmentStart + segments[i].Length;
                if (segmentStart <= written && segmentOverlap > written) {
                    long offset = written - segmentStart;
                    long samplesToWrite = Math.Min(segmentOverlap - offset, blockLength);
                    segments[i].WriteBlock(samples, from, from + samplesToWrite);
                } else {
                    long blockEnd = written + blockLength;
                    if (segmentStart < blockEnd && segmentOverlap > blockEnd) {
                        long samplesToWrite = Math.Min(blockEnd - segmentStart, blockLength);
                        segments[i].WriteBlock(samples, to - samplesToWrite, samplesToWrite);
                        break;
                    }
                }
            }
            written += blockLength;
        }

        /// <summary>
        /// Close the files of the segments.
        /// </summary>
        public override void Dispose() {
            for (int i = 0; i < segments.Length; i++) {
                segments[i].Dispose();
            }
        }
    }
}