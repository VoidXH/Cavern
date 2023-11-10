using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Minimal RIFF wave file reader.
    /// </summary>
    public class RIFFWaveReader : AudioReader {
        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        RIFFWaveDecoder decoder;

        /// <summary>
        /// Minimal RIFF wave file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public RIFFWaveReader(Stream reader) : base(reader) { }

        /// <summary>
        /// Minimal RIFF wave file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public RIFFWaveReader(string path) : base(path) { }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            if (decoder == null) {
                decoder = new RIFFWaveDecoder(reader);
                ChannelCount = decoder.ChannelCount;
                Length = decoder.Length;
                SampleRate = decoder.SampleRate;
                Bits = decoder.Bits;
            }
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to);

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public override void Seek(long sample) {
            if (decoder == null) {
                ReadHeader();
            }
            decoder.Seek(sample);
        }

        /// <summary>
        /// Goes back to a state where the first sample can be read.
        /// </summary>
        public override void Reset() {
            reader.Position = 0;
            if (decoder == null) {
                ReadHeader();
            }
            decoder.Seek(0);
        }

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            return new RIFFWaveRenderer(decoder);
        }
    }
}