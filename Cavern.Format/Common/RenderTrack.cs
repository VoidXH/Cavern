using System;
using System.IO;

using Cavern.Format.Container;

namespace Cavern.Format.Common {
    /// <summary>
    /// Encodes audio content as a <see cref="Track"/> to be used in a <see cref="ContainerWriter"/>.
    /// </summary>
    public class RenderTrack : Track, IDisposable {
        /// <summary>
        /// Seconds that pass with each block.
        /// </summary>
        internal readonly double timeStep;

        /// <summary>
        /// Encodes the audio into <see cref="output"/>.
        /// </summary>
        readonly AudioWriter encoder;

        /// <summary>
        /// Encoded bytes are stored in this stream.
        /// </summary>
        readonly MemoryStream output = new MemoryStream();

        /// <summary>
        /// The reused result of <see cref="ReadNextBlock"/> if the block length doesn't change.
        /// </summary>
        byte[] blockCache = new byte[0];

        /// <summary>
        /// Number of blocks requested by <see cref="ReadNextBlock"/>.
        /// </summary>
        int blocksWritten;

        /// <summary>
        /// Encodes audio content as a <see cref="Track"/> to be used in a <see cref="ContainerWriter"/>.
        /// </summary>
        /// <param name="format">Codec used for encoding</param>
        /// <param name="blockSize">The fixed number of samples that will be encoded (for all channels)</param>
        /// <param name="channelCount">Number of output channels</param>
        /// <param name="length">Content length in samples (for a single channel)</param>
        /// <param name="sampleRate">Rendering environment sample rate</param>
        /// <param name="bits">Bit depth of the <paramref name="format"/> if applicable</param>
        public RenderTrack(Codec format, int blockSize, int channelCount, long length, int sampleRate, BitDepth bits) {
            timeStep = blockSize / channelCount / (double)sampleRate;
            Format = format;
            encoder = format switch {
                Codec.PCM_LE => new RIFFWaveWriter(output, channelCount, length, sampleRate, bits),
                Codec.PCM_Float => new RIFFWaveWriter(output, channelCount, length, sampleRate, bits),
                _ => throw new UnsupportedCodecException(true, format),
            };
            Extra = new TrackExtraAudio {
                SampleRate = encoder.SampleRate,
                ChannelCount = encoder.ChannelCount,
                Bits = encoder.Bits
            };
        }

        /// <summary>
        /// Process a block of samples and put the encoded block in <see cref="output"/> so <see cref="ReadNextBlock"/> could read it.
        /// To use this class, do the following steps:<br />
        /// - create the fixed-size block of samples<br />
        /// - <see cref="EncodeNextBlock(float[])"/><br />
        /// - <see cref="ContainerWriter.WriteBlock(double)"/>, it will call <see cref="ReadNextBlock"/>
        /// </summary>
        public void EncodeNextBlock(float[] samples) => encoder.WriteBlock(samples, 0, samples.Length);

        /// <summary>
        /// The following block of the track is rendered and available.
        /// </summary>
        public override bool IsNextBlockAvailable() => output.Position != 0;

        /// <summary>
        /// Continue reading the track.
        /// </summary>
        public override byte[] ReadNextBlock() {
            if (blockCache.LongLength != output.Position) {
                blockCache = new byte[output.Position];
            }
            output.Position = 0; // For reading
            output.Read(blockCache);
            output.Position = 0; // For overwriting
            blocksWritten++;
            return blockCache;
        }

        /// <summary>
        /// Returns if the next block can be completely decoded by itself.
        /// </summary>
        public override bool IsNextBlockKeyframe() => true;

        /// <summary>
        /// Get the block's offset in seconds.
        /// </summary>
        public override double GetNextBlockOffset() => blocksWritten * timeStep;

        /// <summary>
        /// Free up the resources used by this object.
        /// </summary>
        public void Dispose() => encoder.Dispose();
    }
}