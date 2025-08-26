using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Handles formats where samples are interlaced and uncompressed PCM.
    /// </summary>
    public abstract class UncompressedWriter : AudioWriter {
        /// <summary>
        /// Total samples written to the output file.
        /// </summary>
        protected long SamplesWritten { get; private set; }

        /// <summary>
        /// Reallocated output buffer for block writes.
        /// </summary>
        byte[] outputBuffer = Array.Empty<byte>();

        /// <summary>
        /// Handles formats where samples are interlaced and uncompressed PCM.
        /// </summary>
        protected UncompressedWriter(Stream writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Handles formats where samples are interlaced and uncompressed PCM.
        /// </summary>
        protected UncompressedWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(path, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public sealed override void WriteBlock(float[] samples, long from, long to) {
            const long skip = FormatConsts.blockSize / sizeof(float); // Source split optimization for both memory and IO
            if (to - from > skip) {
                long actualSkip = skip - skip % ChannelCount; // Blocks have to be divisible with the channel count
                for (; from < to; from += actualSkip) {
                    WriteBlock(samples, from, Math.Min(to, from + actualSkip));
                }
                return;
            }

            // Don't allow overwriting
            int window = (int)(to - from);
            SamplesWritten += window / ChannelCount;
            if (SamplesWritten > Length) {
                long extra = SamplesWritten - Length;
                window -= (int)(extra * ChannelCount);
                SamplesWritten -= extra;
            }

            ArrayExtensions.EnsureLength(ref outputBuffer, window * ((long)Bits >> 3));
            switch (Bits) {
                case BitDepth.Int8:
                    for (int i = 0; i < window; i++) {
                        outputBuffer[i] = (byte)(samples[from++] * sbyte.MaxValue);
                    }
                    break;
                case BitDepth.Int16:
                    for (int i = 0; i < window; i++) {
                        short val = (short)(samples[from++] * short.MaxValue);
                        outputBuffer[2 * i] = (byte)val;
                        outputBuffer[2 * i + 1] = (byte)(val >> 8);
                    }
                    break;
                case BitDepth.Int24:
                    for (int i = 0; i < window; i++) {
                        QMath.ConverterStruct val = new QMath.ConverterStruct { asInt = (int)((double)samples[from++] * int.MaxValue) };
                        outputBuffer[3 * i] = val.byte1;
                        outputBuffer[3 * i + 1] = val.byte2;
                        outputBuffer[3 * i + 2] = val.byte3;
                    }
                    break;
                case BitDepth.Float32:
                    for (int i = 0; i < window; i++) {
                        QMath.ConverterStruct val = new QMath.ConverterStruct { asFloat = samples[from++] };
                        outputBuffer[4 * i] = val.byte0;
                        outputBuffer[4 * i + 1] = val.byte1;
                        outputBuffer[4 * i + 2] = val.byte2;
                        outputBuffer[4 * i + 3] = val.byte3;
                    }
                    break;
                default:
                    throw new InvalidBitDepthException(Bits);
            }
            writer.Write(outputBuffer);
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array for a single channel (inclusive)</param>
        /// <param name="to">End position in the input array for a single channel (exclusive)</param>
        public sealed override void WriteBlock(float[][] samples, long from, long to) {
            // Don't allow overwriting
            SamplesWritten += to - from;
            if (SamplesWritten > Length) {
                long extra = SamplesWritten - Length;
                to -= extra;
                SamplesWritten -= extra;
            }

            switch (Bits) {
                case BitDepth.Int8:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny((sbyte)(samples[channel][from] * sbyte.MaxValue));
                        }
                        from++;
                    }
                    break;
                case BitDepth.Int16:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny((short)(samples[channel][from] * short.MaxValue));
                        }
                        from++;
                    }
                    break;
                case BitDepth.Int24:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            int src = (int)(samples[channel][from] * BitConversions.int24Max);
                            writer.WriteAny((short)src);
                            writer.WriteAny((byte)(src >> 16));
                        }
                        from++;
                    }
                    break;
                case BitDepth.Float32:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny(samples[channel][from]);
                        }
                        from++;
                    }
                    break;
                default:
                    throw new InvalidBitDepthException(Bits);
            }
        }
    }
}
