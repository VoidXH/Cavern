using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Container;
using Cavern.Format.Container.Matroska;
using Cavern.Format.Renderers;
using Cavern.Format.Transcoders;

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
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public abstract Renderer GetRenderer();

        /// <summary>
        /// Read the file header.
        /// </summary>
        public abstract void ReadHeader();

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        public abstract void Seek(long sample);

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file. Samples are counted for all channels.</remarks>
        public abstract void ReadBlock(float[] samples, long from, long to);

        /// <summary>
        /// Read a block of samples to a multichannel array.
        /// </summary>
        /// <param name="samples">Input array ([channel][sample])</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file. Samples counted for a single channel.</remarks>
        public virtual void ReadBlock(float[][] samples, long from, long to) {
            long perChannel = to - from, sampleCount = perChannel * samples.LongLength;
            float[] source = new float[sampleCount];
            ReadBlock(source, 0, sampleCount);
            for (long position = 0, sample = 0; sample < perChannel; ++sample)
                for (long channel = 0; channel < samples.LongLength; ++channel)
                    samples[channel][sample] = source[position++];
        }

        /// <summary>
        /// Read the entire file, including the header, and get the data.
        /// </summary>
        public float[] Read() {
            Reset();
            return ReadAfterHeader();
        }

        /// <summary>
        /// Read the entire file, and get the data. The header should have been read before.
        /// </summary>
        public float[] ReadAfterHeader() {
            float[] samples = new float[Length * ChannelCount];
            ReadBlock(samples, 0, samples.Length);
            Dispose();
            return samples;
        }

        /// <summary>
        /// Read the entire file, including the header, and pack it in a <see cref="Clip"/>.
        /// </summary>
        public Clip ReadClip() {
            Reset();
            return ReadClipAfterHeader();
        }

        /// <summary>
        /// Read the entire file and pack it in a <see cref="Clip"/>. The header should have been read before.
        /// </summary>
        public Clip ReadClipAfterHeader() => new Clip(ReadAfterHeader(), ChannelCount, SampleRate);

        /// <summary>
        /// Read the entire file, including the header.
        /// </summary>
        public float[][] ReadMultichannel() {
            Reset();
            return ReadMultichannelAfterHeader();
        }

        /// <summary>
        /// Read the entire file. The header should have been read before.
        /// </summary>
        public float[][] ReadMultichannelAfterHeader() {
            float[][] samples = new float[ChannelCount][];
            for (int channel = 0; channel < ChannelCount; ++channel)
                samples[channel] = new float[Length];
            ReadBlock(samples, 0, Length);
            Dispose();
            return samples;
        }

        /// <summary>
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public virtual void Reset() {
            reader.BaseStream.Position = 0;
            ReadHeader();
        }

        /// <summary>
        /// Tests if the next rolling byte block is as expected, if not, it advances by 1 byte.
        /// </summary>
        protected bool RollingBlockCheck(byte[] cache, byte[] block) {
            for (int i = 1; i < cache.Length; ++i)
                cache[i - 1] = cache[i];
            cache[^1] = reader.ReadByte();
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
        public virtual void Dispose() => reader?.Close();

        /// <summary>
        /// Open an audio stream for reading. The format will be detected automatically.
        /// </summary>
        public static AudioReader Open(BinaryReader reader) {
            int syncWord = reader.ReadInt32();
            reader.BaseStream.Position = 0;
            if ((syncWord & 0xFFFF) == EnhancedAC3.syncWordLE)
                return new EnhancedAC3Reader(reader);
            return syncWord switch {
                RIFFWave.syncWord1 => new RIFFWaveReader(reader),
                LimitlessAudioFormat.syncWord => new LimitlessAudioFormatReader(reader),
                MatroskaTree.EBML => new AudioTrackReader(new MatroskaReader(reader.BaseStream).GetMainAudioTrack(), true),
                _ => throw new UnsupportedFormatException(),
            };
        }

        /// <summary>
        /// Open an audio file for reading by file name. The format will be detected automatically.
        /// </summary>
        public static AudioReader Open(string path) => Open(new BinaryReader(File.OpenRead(path)));

        /// <summary>
        /// Open an audio clip from a stream. The format will be detected automatically.
        /// </summary>
        public static Clip ReadClip(BinaryReader reader) => Open(reader).ReadClip();

        /// <summary>
        /// Open an audio clip by file name. The format will be detected automatically.
        /// </summary>
        public static Clip ReadClip(string path) => Open(path).ReadClip();
    }
}