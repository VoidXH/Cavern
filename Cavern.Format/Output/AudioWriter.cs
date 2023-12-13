using System;
using System.IO;

using Cavern.Format.Consts;
using Cavern.Format.Container;

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
        /// Output length in samples per channel.
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
        /// Stream to write to.
        /// </summary>
        protected Stream writer;

        /// <summary>
        /// Abstract audio stream writer.
        /// </summary>
        /// <param name="writer">Stream to write to</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        protected AudioWriter(Stream writer, int channelCount, long length, int sampleRate, BitDepth bits) {
            this.writer = writer;
            ChannelCount = channelCount;
            Length = length;
            SampleRate = sampleRate;
            Bits = bits;
        }

        /// <summary>
        /// Abstract audio file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        protected AudioWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            this(File.Open(path, FileMode.Create), channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Create an <see cref="AudioWriter"/> that matches the output file name.
        /// </summary>
        /// <remarks>When creating a .LAF file, the active environment's channels will be used.</remarks>
        /// <param name="path">Output file name</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public static AudioWriter Create(string path, int channelCount, long length, int sampleRate, BitDepth bits) =>
            path[^3..] switch {
                "laf" => new LimitlessAudioFormatWriter(path, length, sampleRate, bits, Listener.Channels),
                "wav" => new RIFFWaveWriter(path, channelCount, length, sampleRate, bits),
                _ => null,
            };

        /// <summary>
        /// Open a file stream optimized for sequential writing.
        /// </summary>
        internal static Stream Open(string path) =>
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, FormatConsts.blockSize, FileOptions.SequentialScan);

        /// <summary>
        /// Create the file header.
        /// </summary>
        public abstract void WriteHeader();

        /// <summary>
        /// Write a block of mono or interlaced samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public abstract void WriteBlock(float[] samples, long from, long to);

        /// <summary>
        /// Write a block of multichannel samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>If you don't want to implement a custom, optimized version, use the implementation in
        /// <see cref="AudioWriterIntoContainer"/>.</remarks>
        public abstract void WriteBlock(float[][] samples, long from, long to);

        /// <summary>
        /// Write the first selected channels of a block of mono or interlaced samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="channelLimit">Allow only this many channels to be written</param>
        /// <param name="channels">Channel count of the input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>This function is destructive, <paramref name="samples"/> will be unusable after the operation.</remarks>
        public virtual void WriteChannelLimitedBlock(float[] samples, int channelLimit, int channels, long from, long to) {
            long targetSample = from;
            for (long sourceSample = from; sourceSample < to; sourceSample += channels) {
                for (int channel = 0; channel < channelLimit; channel++) {
                    samples[targetSample++] = samples[sourceSample + channel];
                }
            }
            to = from + (to - from) / channels * channelLimit;
            WriteBlock(samples, from, to);
        }

        /// <summary>
        /// Write the entire mono file, including the header.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[] samples) {
            Length = samples.LongLength / ChannelCount;
            WriteHeader();
            WriteBlock(samples, 0, samples.LongLength);
            Dispose();
        }

        /// <summary>
        /// Write the entire multichannel file.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[][] samples) {
            Length = samples[0].LongLength;
            WriteHeader();
            WriteBlock(samples, 0, samples[0].LongLength);
            Dispose();
        }

        /// <summary>
        /// Writes the <paramref name="samples"/> to be played back channel after channel.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void WriteOffset(float[][] samples) => WriteOffset(samples, samples.Length);

        /// <summary>
        /// Writes the <paramref name="samples"/> to be played back channel after channel, but some channels play simultaneously.
        /// </summary>
        /// <param name="samples">All input samples</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public void WriteOffset(float[][] samples, int period) {
            ChannelCount = samples.Length;
            Length = period * samples[0].Length;
            float[] empty = new float[samples[0].Length];
            float[][] holder = new float[samples.Length][];
            WriteHeader();
            for (int curPeriod = 0; curPeriod < period; curPeriod++) {
                for (int channel = 0; channel < holder.Length; channel++) {
                    holder[channel] = channel % period == curPeriod ? samples[channel] : empty;
                }
                WriteBlock(holder, 0, holder[0].Length);
            }
            Dispose();
        }

        /// <summary>
        /// Writes the <paramref name="samples"/> to be played back channel after channel.
        /// </summary>
        /// <param name="samples">All input samples</param>
        /// <param name="channelCount">Output channel count</param>
        public void WriteForEachChannel(float[] samples, int channelCount) {
            float[][] holder = new float[channelCount][];
            for (int channel = 0; channel < channelCount; channel++) {
                holder[channel] = samples;
            }
            WriteOffset(holder);
        }

        /// <summary>
        /// Close the writer.
        /// </summary>
        public virtual void Dispose() {
            if (writer != null) {
                writer.Close();
                writer = null;
            }
        }
    }
}