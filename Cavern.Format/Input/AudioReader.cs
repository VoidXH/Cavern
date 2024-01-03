using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Container;
using Cavern.Format.Container.Matroska;
using Cavern.Format.Renderers;
using Cavern.Format.Transcoders;
using Cavern.Format.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Abstract audio file reader.
    /// </summary>
    public abstract class AudioReader : IDisposable {
        /// <summary>
        /// Filter to all supported file types for open file dialogs. These are the containers supported by <see cref="Open(string)"/>.
        /// </summary>
        public static readonly string filter = "*.ac3;*.eac3;*.ec3;*.laf;*.m4a;*.m4v;*.mka;*.mkv;*.mov;*.mp4;*.mxf;*.qt;*.wav;*.weba;*.webm";

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
        /// Path to the opened file or null if the audio is not read from a file.
        /// </summary>
        public string Path => (reader as FileStream)?.Name;

        /// <summary>
        /// File reader object.
        /// </summary>
        protected Stream reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        protected AudioReader(Stream reader) => this.reader = reader;

        /// <summary>
        /// Abstract audio file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        protected AudioReader(string path) => reader = OpenSequentialStream(path);

        /// <summary>
        /// Open an audio stream for reading. The format will be detected automatically.
        /// </summary>
        public static AudioReader Open(Stream reader) {
            int syncWord = reader.ReadInt32();
            if (reader.ReadInt32BE() == MP4Consts.fileTypeBox) {
                reader.Position = 0;
                return OpenContainer(new MP4Reader(reader));
            }
            reader.Position = 0;
            if ((syncWord & 0xFFFF) == EnhancedAC3.syncWordLE) {
                return new EnhancedAC3Reader(reader);
            }

            switch (syncWord) {
                case RIFFWave.syncWord1:
                case RIFFWave.syncWord1_64:
                    return new RIFFWaveReader(reader);
                case LimitlessAudioFormat.syncWord:
                    return new LimitlessAudioFormatReader(reader);
                case MatroskaTree.EBML_LE:
                    return OpenContainer(new MatroskaReader(reader));
                case MXFConsts.universalLabel:
                    return OpenContainer(new MXFReader(reader));
                default:
                    throw new UnsupportedFormatException();
            }
        }

        /// <summary>
        /// Open an audio file for reading by file name. The format will be detected automatically.
        /// </summary>
        public static AudioReader Open(string path) => Open(OpenSequentialStream(path));

        /// <summary>
        /// Open an audio clip from a stream. The format will be detected automatically.
        /// </summary>
        public static Clip ReadClip(Stream reader) => Open(reader).ReadClip();

        /// <summary>
        /// Open an audio clip by file name. The format will be detected automatically.
        /// </summary>
        public static Clip ReadClip(string path) => Open(path).ReadClip();

        /// <summary>
        /// Open a file stream optimized for sequential reading.
        /// </summary>
        internal static Stream OpenSequentialStream(string path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, FormatConsts.blockSize, FileOptions.SequentialScan);

        /// <summary>
        /// Open a container as a single audio track by selecting the best fit of its tracks for Cavern.Format.
        /// </summary>
        static AudioReader OpenContainer(ContainerReader reader) {
            Track track = reader.GetMainAudioTrack();
            return track == null ? throw new NoProgramException() : new AudioTrackReader(track, true);
        }

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
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
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
            for (long position = 0, sample = 0; sample < perChannel; ++sample) {
                for (long channel = 0; channel < samples.LongLength; ++channel) {
                    samples[channel][sample] = source[position++];
                }
            }
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
            for (int channel = 0; channel < ChannelCount; ++channel) {
                samples[channel] = new float[Length];
            }
            ReadBlock(samples, 0, Length);
            Dispose();
            return samples;
        }

        /// <summary>
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public virtual void Reset() {
            reader.Position = 0;
            ReadHeader();
        }

        /// <summary>
        /// Close the reader.
        /// </summary>
        public virtual void Dispose() => reader?.Close();
    }
}