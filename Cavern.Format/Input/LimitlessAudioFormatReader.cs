using System;
using System.IO;

using Cavern.Format.Consts;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Minimal Limitless Audio Format file reader.
    /// </summary>
    public class LimitlessAudioFormatReader : AudioReader {
        /// <summary>
        /// Description of each imported channel/object.
        /// </summary>
        public Channel[] Channels { get; protected set; }

        /// <summary>
        /// Maximum size of each read block. This can balance optimization between memory and IO.
        /// </summary>
        const long skip = 10 /* MB */ * 1024 * 1024 / sizeof(float);

        /// <summary>
        /// Bytes used before each second of samples to determine which channels are actually exported.
        /// </summary>
        int layoutByteCount;

        /// <summary>
        /// Minimal Limitless Audio Format file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public LimitlessAudioFormatReader(BinaryReader reader) : base(reader) { }

        /// <summary>
        /// Minimal Limitless Audio Format file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public LimitlessAudioFormatReader(string path) : base(path) { }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            BlockTest(LimitlessAudioFormat.limitless); // Find Limitless marker
            byte[] cache = new byte[LimitlessAudioFormat.head.Length];
            while (!RollingBlockCheck(cache, LimitlessAudioFormat.head)) ; // Find header marker, skip metadata

            Bits = reader.ReadByte() switch {
                (byte)LAFMode.Int8 => BitDepth.Int8,
                (byte)LAFMode.Int16 => BitDepth.Int16,
                (byte)LAFMode.Int24 => BitDepth.Int24,
                (byte)LAFMode.Float32 => BitDepth.Float32,
                _ => throw new IOException("Unsupported LAF quality mode.")
            };
            reader.ReadByte(); // Channel mode indicator (skipped)
            ChannelCount = reader.ReadInt32();
            layoutByteCount = ChannelCount % 8 == 0 ? ChannelCount >> 3 : ((ChannelCount >> 3) + 1);
            Channels = new Channel[ChannelCount];
            for (int channel = 0; channel < ChannelCount; ++channel)
                Channels[channel] = new Channel(reader.ReadSingle(), reader.ReadSingle(), reader.ReadByte() != 0);
            SampleRate = reader.ReadInt32();
            Length = reader.ReadInt64();
        }

        /// <summary>
        /// Samples read for each channel since the construction of this reader.
        /// </summary>
        long readSamples = 0;

        /// <summary>
        /// Read position in <see cref="lastReadSecond"/>.
        /// </summary>
        int copiedSamples = 0;

        /// <summary>
        /// The last loaded second, as LAF stores channel availability data every second.
        /// </summary>
        float[][] lastReadSecond;

        /// <summary>
        /// Read the next second of audio data.
        /// </summary>
        void ReadSecond() {
            if (lastReadSecond != null) {
                for (int channel = 0; channel < ChannelCount; ++channel)
                    Array.Clear(lastReadSecond[channel], 0, SampleRate);
            } else {
                lastReadSecond = new float[ChannelCount][];
                for (int channel = 0; channel < ChannelCount; ++channel)
                    lastReadSecond[channel] = new float[SampleRate];
            }

            byte[] layoutBytes = reader.ReadBytes(layoutByteCount);
            if (layoutBytes.Length == 0)
                return;
            bool[] writtenChannels = new bool[ChannelCount];
            int channelsToRead = 0;
            for (int channel = 0; channel < ChannelCount; ++channel)
                if (writtenChannels[channel] = (layoutBytes[channel >> 3] >> (channel % 8)) % 2 != 0)
                    ++channelsToRead;

            int samplesToRead = (int)Math.Min(Length - readSamples, SampleRate);
            int bytesToRead = samplesToRead * channelsToRead * ((int)Bits >> 3);
            byte[] source = reader.ReadBytes(bytesToRead);
            int sourcePos = 0;

            switch (Bits) {
                case BitDepth.Int8:
                    for (int sample = 0; sample < samplesToRead; ++sample)
                        for (int channel = 0; channel < ChannelCount; ++channel)
                            if (writtenChannels[channel])
                                lastReadSecond[channel][sample] = source[sourcePos++] * BitConversions.fromInt8;
                    break;
                case BitDepth.Int16:
                    for (int sample = 0; sample < samplesToRead; ++sample)
                        for (int channel = 0; channel < ChannelCount; ++channel)
                            if (writtenChannels[channel])
                                lastReadSecond[channel][sample] = (short)(source[sourcePos++] | source[sourcePos++] << 8) *
                                    BitConversions.fromInt16;
                    break;
                case BitDepth.Int24:
                    for (int sample = 0; sample < samplesToRead; ++sample)
                        for (int channel = 0; channel < ChannelCount; ++channel)
                            if (writtenChannels[channel])
                                lastReadSecond[channel][sample] = // This needs to be shifted into overflow for correct sign
                                    ((source[sourcePos++] << 8 | source[sourcePos++] << 16 | source[sourcePos++] << 24) >> 8) *
                                    BitConversions.fromInt24;
                    break;
                case BitDepth.Float32:
                    for (int sample = 0; sample < samplesToRead; ++sample) {
                        for (int channel = 0; channel < ChannelCount; ++channel) {
                            if (writtenChannels[channel]) {
                                lastReadSecond[channel][sample] = BitConverter.ToSingle(source, sourcePos);
                                sourcePos += sizeof(float);
                            }
                        }
                    }
                    break;
            }

            readSamples += SampleRate;
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file. Samples are counted for all channels.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) {
            if (to - from > skip) {
                for (; from < to; from += skip)
                    ReadBlock(samples, from, Math.Min(to, from + skip));
                return;
            }

            while (from < to) {
                if (copiedSamples == 0)
                    ReadSecond();
                int toProcess = (int)Math.Min((to - from) / ChannelCount, SampleRate - copiedSamples);

                for (int sample = 0; sample < toProcess; ++sample)
                    for (int channel = 0; channel < ChannelCount; ++channel)
                        samples[from++] = lastReadSecond[channel][copiedSamples + sample];

                copiedSamples += toProcess;
                if (copiedSamples == SampleRate)
                    copiedSamples = 0;
                from += toProcess * ChannelCount;
            }
        }

        /// <summary>
        /// Read a block of samples to a multichannel array.
        /// </summary>
        /// <param name="samples">Input array ([channel][sample])</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file. Samples counted for a single channel.</remarks>
        public override void ReadBlock(float[][] samples, long from, long to) {
            if (to - from > skip) {
                for (; from < to; from += skip)
                    ReadBlock(samples, from, Math.Min(to, from + skip));
                return;
            }

            while (from < to) {
                if (copiedSamples == 0)
                    ReadSecond();
                int toProcess = (int)Math.Min(to - from, SampleRate - copiedSamples);

                for (int sample = 0; sample < toProcess; ++sample)
                    for (int channel = 0; channel < ChannelCount; ++channel)
                        samples[channel][from + sample] = lastReadSecond[channel][copiedSamples + sample];

                copiedSamples += toProcess;
                if (copiedSamples == SampleRate)
                    copiedSamples = 0;
                from += toProcess;
            }
        }

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) {
            throw new NotImplementedException();
        }
    }
}