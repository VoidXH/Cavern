using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>
    /// Abstract audio file writer.
    /// </summary>
    public abstract class AudioWriter : IDisposable {
        /// <summary>
        /// Output channel count.
        /// </summary>
        public int ChannelCount { get; protected set; }

        /// <summary>
        /// Output length in samples.
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// Output sample rate.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Output bit depth.
        /// </summary>
        public BitDepth Bits { get; protected set; }

        /// <summary>
        /// File writer object.
        /// </summary>
        protected BinaryWriter writer;

        /// <summary>
        /// Abstract audio file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public AudioWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits) {
            this.writer = writer;
            ChannelCount = channelCount;
            Length = length;
            SampleRate = sampleRate;
            Bits = bits;
        }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public abstract void WriteHeader();

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public abstract void WriteBlock(float[] samples, long from, long to);

        /// <summary>
        /// Write the entire file.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[] samples) {
            Length = samples.LongLength / ChannelCount;
            WriteHeader();
            WriteBlock(samples, 0, samples.LongLength);
            writer.Close();
        }

        /// <summary>
        /// Close the writer.
        /// </summary>
        public void Dispose() {
            if (writer != null)
                writer.Close();
        }
    }
}