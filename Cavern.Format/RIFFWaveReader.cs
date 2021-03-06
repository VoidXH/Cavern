﻿using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal RIFF wave file reader.</summary>
    public class RIFFWaveReader : AudioReader {
        /// <summary>Minimal RIFF wave file reader.</summary>
        /// <param name="reader">File reader object</param>
        public RIFFWaveReader(BinaryReader reader) : base(reader) { }

        /// <summary>Read the file header.</summary>
        public override void ReadHeader() {
            // RIFF header
            BlockTest(RIFFWaveUtils.RIFF);
            reader.ReadInt32(); // File length
            BlockTest(RIFFWaveUtils.WAVE);

            // Format header
            BlockTest(RIFFWaveUtils.fmt);
            reader.ReadInt32(); // Format header length
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float, -2 = WAVE EX
            ChannelCount = reader.ReadInt16();
            SampleRate = reader.ReadInt32();
            reader.ReadInt32(); // Bytes/sec
            reader.ReadInt16(); // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == -2) {
                reader.ReadInt16(); // Extension size (22)
                reader.ReadInt16(); // Valid bits per sample
                reader.ReadInt32(); // Channel mask
                sampleFormat = reader.ReadInt16();
                reader.BaseStream.Position += 15; // Skip the rest of the sub format GUID
            }
            if (sampleFormat == 1) {
                if (bitDepth == 8)
                    Bits = BitDepth.Int8;
                else if (bitDepth == 16)
                    Bits = BitDepth.Int16;
                else if (bitDepth == 24)
                    Bits = BitDepth.Int24;
                else
                    throw new IOException(string.Format("Unsupported bit depth for signed little endian integer: {0}.", bitDepth));
            } else if (sampleFormat == 3 && bitDepth == 32)
                Bits = BitDepth.Float32;
            else
                throw new IOException(string.Format("Unsupported bit depth ({0}) for sample format {1}.", bitDepth, sampleFormat));

            // Data header
            byte[] cache = new byte[RIFFWaveUtils.data.Length];
            while (!RollingBlockCheck(cache, RIFFWaveUtils.data)) ;
            Length = reader.ReadInt32() * 8 / (int)Bits / ChannelCount;
        }

        /// <summary>Read a block of samples.</summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) {
            const long skip = 10 * 1024 * 1024 / sizeof(float); // 10 MB source splits at max to optimize for both memory and IO
            const float fromInt8 = 1 / 128f, fromInt16 = 1 / 32767f, fromInt24 = 1 / 8388608f;
            if (to - from > skip) {
                for (; from < to; from += skip)
                    ReadBlock(samples, from, Math.Min(to, from + skip));
                return;
            }
            switch (Bits) {
                case BitDepth.Int8: {
                        byte[] source = reader.ReadBytes((int)(to - from));
                        for (int i = 0; i < source.Length; ++i)
                            samples[from++] = source[i] * fromInt8 - 1f;
                        break;
                    }
                case BitDepth.Int16: {
                        byte[] source = reader.ReadBytes((int)(to - from) * sizeof(short));
                        for (int i = 0; i < source.Length;)
                            samples[from++] = (short)(source[i++] + source[i++] * 256) * fromInt16;
                        break;
                    }
                case BitDepth.Int24: {
                        byte[] source = reader.ReadBytes((int)(to - from) * 3);
                        for (int i = 0; i < source.Length;)
                            samples[from++] = ((source[i++] << 8) | (source[i++] << 16) | (source[i++] << 24)) / 256 * fromInt24;
                        break;
                    }
                case BitDepth.Float32: {
                        if (from < int.MaxValue / sizeof(float)) {
                            int count = (int)(to - from) * sizeof(float);
                            byte[] source = reader.ReadBytes(count);
                            Buffer.BlockCopy(source, 0, samples, (int)from * sizeof(float), count);
                        } else while (from < to)
                                samples[from++] = reader.ReadSingle();
                        break;
                    }
            }
        }
    }
}