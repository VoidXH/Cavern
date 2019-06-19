using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal Limitless Audio Format file writer.</summary>
    public class LimitlessAudioFormatWriter : AudioWriter {
        /// <summary>Limitless Audio Format indicator starting bytes.</summary>
        static readonly byte[] limitless = new byte[9] { (byte)'L', (byte)'I', (byte)'M', (byte)'I', (byte)'T', (byte)'L', (byte)'E', (byte)'S', (byte)'S' };
        /// <summary>Header marker bytes.</summary>
        static readonly byte[] head = new byte[4] { (byte)'H', (byte)'E', (byte)'A', (byte)'D', };

        /// <summary>Output channel information.</summary>
        readonly Channel[] channels;
        /// <summary>The past second for each channel.</summary>
        readonly float[] cache;
        /// <summary>Write position in the <see cref="cache"/>. Used to check if the cache is full for block dumping.</summary>
        int cachePosition = 0;
        /// <summary>Total samples written in the file so far. Used to check the end of file and dump the unfilled last block.</summary>
        long totalWritten = 0;

        /// <summary>Minimal Limitless Audio Format file writer.</summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        public LimitlessAudioFormatWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits, Channel[] channels) :
            base(writer, channelCount, length, sampleRate, bits) {
            this.channels = channels;
            cache = new float[channelCount * sampleRate];
        }

        /// <summary>Create the file header.</summary>
        public override void WriteHeader() {
            writer.Write(limitless); // Limitless marker
            // No custom headers
            writer.Write(head); // Main header marker
            writer.Write(new byte[] { bits == BitDepth.Int8 ? (byte)0 : (bits == BitDepth.Int16 ? (byte)1 : (byte)2), (byte)0 }); // Quality and channel mode indicator
            writer.Write(BitConverter.GetBytes(channelCount)); // Channel/object count
            for (int channel = 0; channel < channelCount; ++channel) { // Channel/object info
                writer.Write(BitConverter.GetBytes(channels[channel].X)); // Rotation on X axis
                writer.Write(BitConverter.GetBytes(channels[channel].Y)); // Rotation on Y axis
                writer.Write(channels[channel].LFE ? (byte)1 : (byte)0); // Low frequency
            }
            writer.Write(BitConverter.GetBytes(sampleRate));
            writer.Write(BitConverter.GetBytes(length));
        }

        /// <summary>Output only the used channels from the last second.</summary>
        /// <param name="until">Samples to dump from the <see cref="cache"/></param>
        void DumpBlock(long until) {
            bool[] toWrite = new bool[channelCount];
            for (int channel = 0; channel < channelCount; ++channel)
                for (int sample = channel; !toWrite[channel] && sample < sampleRate; sample += channelCount)
                    if (cache[sample] != 0)
                        toWrite[channel] = true;
            byte[] layoutBytes = new byte[channelCount % 8 == 0 ? channelCount / 8 : (channelCount / 8 + 1)];
            for (int channel = 0; channel < channelCount; ++channel) {
                if (toWrite[channel])
                    layoutBytes[channel / 8] += (byte)(1 << (channel % 8));
            }
            writer.Write(layoutBytes);
            switch (bits) {
                case BitDepth.Int8:
                    for (int sample = 0; sample < until; ++sample)
                        if (toWrite[sample % channelCount])
                            writer.Write((byte)((cache[sample] + 1f) * 127f));
                    break;
                case BitDepth.Int16:
                    for (int sample = 0; sample < until; ++sample)
                        if (toWrite[sample % channelCount])
                            writer.Write(BitConverter.GetBytes((short)(cache[sample] * 32767f)));
                    break;
                case BitDepth.Float32:
                    for (int sample = 0; sample < until; ++sample)
                        if (toWrite[sample % channelCount])
                            writer.Write(BitConverter.GetBytes(cache[sample]));
                    break;
            }
            cachePosition = 0;
        }

        /// <summary>Write a block of samples.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            long dumpLength = to - from;
            while (from < to) {
                for (; from < to && cachePosition < cache.Length; ++from)
                    cache[cachePosition++] = samples[from];
                if (cachePosition == cache.Length)
                    DumpBlock(cache.Length);
            }
            if ((totalWritten += dumpLength) == length)
                DumpBlock(cachePosition);
        }
    }
}