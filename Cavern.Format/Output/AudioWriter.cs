﻿using System;
using System.IO;

using Cavern.Utilities;

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
        /// File writer object.
        /// </summary>
        protected BinaryWriter writer;

        /// <summary>
        /// Abstract audio file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
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
        /// Abstract audio file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public AudioWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            this(new BinaryWriter(File.OpenWrite(path)), channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public abstract void WriteHeader();

        /// <summary>
        /// Write a block of mono samples.
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
        public virtual void WriteBlock(float[][] samples, long from, long to) {
            float[] cache = WaveformUtils.MultichannelToInterlaced(samples, from, to);
            WriteBlock(cache, 0, cache.Length);
        }

        /// <summary>
        /// Write the entire mono file.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[] samples) {
            Length = samples.LongLength / ChannelCount;
            WriteHeader();
            WriteBlock(samples, 0, samples.LongLength);
            writer.Close();
        }

        /// <summary>
        /// Write the entire multichannel file.
        /// </summary>
        /// <param name="samples">All input samples</param>
        public void Write(float[][] samples) {
            Length = samples[0].LongLength;
            WriteHeader();
            WriteBlock(samples, 0, samples[0].LongLength);
            writer.Close();
        }

        /// <summary>
        /// Writes the <paramref name="samples"/> to be played back channel after channel.
        /// </summary>
        /// <param name="samples">All input samples</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public void WriteOffset(float[][] samples, int period = -1) {
            ChannelCount = samples.Length;
            if (period == -1)
                period = ChannelCount;
            Length = period * samples[0].Length;
            float[] empty = new float[samples[0].Length];
            float[][] holder = new float[samples.Length][];
            WriteHeader();
            for (int curPeriod = 0; curPeriod < period; ++curPeriod) {
                for (int channel = 0; channel < holder.Length; ++channel)
                    holder[channel] = channel % period == curPeriod ? samples[channel] : empty;
                WriteBlock(holder, 0, holder[0].Length);
            }
            writer.Dispose();
        }

        /// <summary>
        /// Writes the <paramref name="samples"/> to be played back channel after channel.
        /// </summary>
        /// <param name="samples">All input samples</param>
        /// <param name="channelCount">Output channel count</param>
        public void WriteForEachChannel(float[] samples, int channelCount) {
            float[][] holder = new float[channelCount][];
            for (int channel = 0; channel < channelCount; ++channel)
                holder[channel] = samples;
            WriteOffset(holder);
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