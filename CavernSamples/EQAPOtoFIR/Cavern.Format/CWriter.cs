using System;
using System.IO;
using System.Text;

using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Exports the samples to a C file, into an array.
    /// </summary>
    public class CWriter : AudioWriter {
        /// <summary>
        /// Exports the samples to a C file, into an array.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public CWriter(Stream writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() {
            // Abstract function, but not required operation
        }

        /// <summary>
        /// Write a block of mono or interlaced samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            StringBuilder output = new StringBuilder();
            switch (Bits) {
                case BitDepth.Int8: {
                    byte[] temp = new byte[to - from];
                    for (long i = from; i < to; i++) {
                        temp[i - from] = (byte)((samples[i] + 1) * 127f);
                    }
                    WriteBlock(output, temp, samples.Length, from, to, "unsigned char");
                    break;
                }
                case BitDepth.Int16: {
                    short[] temp = new short[to - from];
                    for (long i = from; i < to; i++) {
                        temp[i - from] = (short)(samples[i] * 32767f);
                    }
                    WriteBlock(output, temp, samples.Length, from, to, "short");
                    break;
                }
                case BitDepth.Int24: {
                    int[] temp = new int[to - from];
                    for (long i = from; i < to; i++) {
                        temp[i - from] = (int)(samples[i] * 8388607f);
                    }
                    WriteBlock(output, temp, samples.Length, from, to, "int");
                    break;
                }
                case BitDepth.Float32: {
                    float[] temp = new float[to - from];
                    Array.Copy(samples, 0, temp, from, temp.Length);
                    WriteBlock(output, temp, samples.Length, from, to, "float");
                    break;
                }
                default:
                    throw new InvalidBitDepthException(Bits);
            }
            string result = output.ToString();
            for (int i = 0; i < result.Length; i++) {
                writer.WriteByte((byte)result[i]);
            }
        }

        /// <summary>
        /// Write a block of multichannel samples.
        /// </summary>
        /// <remarks>This function is not supported.</remarks>
        public override void WriteBlock(float[][] samples, long from, long to) =>
            throw new IOException("Only single-channel data can be exported to an array.");

        /// <summary>
        /// Converts an array of data to a stringified C array.
        /// </summary>
        static void WriteBlock<T>(StringBuilder output, T[] samples, long sourceLength, long from, long to, string typename)
            where T : IComparable {
            if (from == 0) {
                output.Append($"{typename} samples[{sourceLength / (to - from)}][{to - from}] = {{");
            }
            ArrayExtensions.RemoveZeros(ref samples);
            output.Append(System.Environment.NewLine).Append("\t{ ").Append(string.Join(", ", samples)).Append(" },");
            if (to == sourceLength) {
                output.Append(System.Environment.NewLine).Append("};");
            }
        }
    }
}