using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>
    /// Abstract audio file reader.
    /// </summary>
    public abstract class AudioReader : IDisposable {
        /// <summary>
        /// Content channel count.
        /// </summary>
        public int ChannelCount { get; protected set; }

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// Content sample rate.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Content bit depth.
        /// </summary>
        public BitDepth Bits { get; protected set; }

        /// <summary>
        /// File reader object.
        /// </summary>
        protected BinaryReader reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public AudioReader(BinaryReader reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public AudioReader(string path) => reader = new BinaryReader(File.OpenRead(path));

        /// <summary>
        /// Read the file header.
        /// </summary>
        public abstract void ReadHeader();

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.</remarks>
        public abstract void ReadBlock(float[] samples, long from, long to);

        /// <summary>
        /// Read a block of samples to a multichannel array.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.</remarks>
        public virtual void ReadBlock(float[][] samples, long from, long to) {
            long perChannel = to - from, sampleCount = perChannel * samples.LongLength;
            float[] source = new float[sampleCount];
            ReadBlock(source, 0, sampleCount);
            for (long position = 0, sample = 0; sample < perChannel; ++sample)
                for (long channel = 0; channel < samples.LongLength; ++channel)
                    samples[channel][sample] = source[position++];
        }

        /// <summary>
        /// Read the entire file.
        /// </summary>
        public float[] Read() {
            ReadHeader();
            float[] samples = new float[Length * ChannelCount];
            ReadBlock(samples, 0, samples.Length);
            Dispose();
            return samples;
        }

        /// <summary>
        /// Read the entire file and pack it in a <see cref="Clip"/>.
        /// </summary>
        public Clip ReadClip() => new Clip(Read(), ChannelCount, SampleRate);

        /// <summary>Read the entire file.</summary>
        public float[][] ReadMultichannel() {
            ReadHeader();
            float[][] samples = new float[ChannelCount][];
            for (int channel = 0; channel < ChannelCount; ++channel)
                samples[channel] = new float[Length];
            ReadBlock(samples, 0, Length);
            Dispose();
            return samples;
        }

        /// <summary>
        /// Tests if the next rolling byte block is as expected, if not, it advances by 1 byte.
        /// </summary>
        protected bool RollingBlockCheck(byte[] cache, byte[] block) {
            for (int i = 1; i < cache.Length; ++i)
                cache[i - 1] = cache[i];
            cache[cache.Length - 1] = reader.ReadByte();
            for (int i = 0; i < block.Length; ++i)
                if (cache[i] != block[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Tests if the next byte block is as expected, throws an exception if it's not.
        /// </summary>
        protected void BlockTest(byte[] block) {
            byte[] input = reader.ReadBytes(block.Length);
            for (int i = 0; i < block.Length; ++i)
                if (input[i] != block[i])
                    throw new IOException("Format mismatch.");
        }

        /// <summary>
        /// Close the reader.
        /// </summary>
        public void Dispose() {
            if (reader != null)
                reader.Close();
        }
    }
}