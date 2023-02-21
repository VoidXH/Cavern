using System.IO;

using Cavern.Format.Decoders;
using Cavern.Format.Renderers;

namespace Cavern.Format {
    /// <summary>
    /// Minimal Limitless Audio Format file reader.
    /// </summary>
    public class LimitlessAudioFormatReader : AudioReader {
        /// <summary>
        /// Bitsteam interpreter.
        /// </summary>
        LimitlessAudioFormatDecoder decoder;

        /// <summary>
        /// Minimal Limitless Audio Format file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public LimitlessAudioFormatReader(Stream reader) : base(reader) { }

        /// <summary>
        /// Minimal Limitless Audio Format file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public LimitlessAudioFormatReader(string path) : base(path) { }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            if (decoder == null) {
                decoder = new LimitlessAudioFormatDecoder(reader);
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
        /// <remarks>The next to - from samples will be read from the file. Samples are counted for all channels.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) => decoder.DecodeBlock(samples, from, to);

        /// <summary>
        /// Get an object-based renderer for this audio file.
        /// </summary>
        public override Renderer GetRenderer() {
            if (decoder == null) {
                ReadHeader();
            }
            return new LimitlessAudioFormatRenderer(decoder);
        }

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
    }
}