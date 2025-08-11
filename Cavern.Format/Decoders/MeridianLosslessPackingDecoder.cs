using System;

using Cavern.Channels;
using Cavern.Format.Decoders.MeridianLosslessPacking;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a Meridian Lossless Packing (MLP) stream into raw decoded samples.
    /// </summary>
    public class MeridianLosslessPackingDecoder : FrameBasedDecoder {
        /// <summary>
        /// Bed channels of the track: either the actual channels or the downmix of spatial data.
        /// </summary>
        public ReferenceChannel[] Beds { get; private set; }

        /// <summary>
        /// If the stream carries a <=16 channel presentation, this is the actually used channel count, otherwise 0.
        /// </summary>
        public int FullChannelCount { get; private set; }

        /// <summary>
        /// Converts a Meridian Lossless Packing (MLP) stream into raw decoded samples.
        /// </summary>
        public MeridianLosslessPackingDecoder(BlockBuffer<byte> reader) : base(reader) {
            Length = -1;
            Bits = BitDepth.Int24;
        }

        /// <inheritdoc/>
        protected override float[] DecodeFrame() {
            MLPHeader header = new MLPHeader();
            header.Decode(reader);
            Beds = header.Beds;
            FullChannelCount = header.FullChannelCount;
            ChannelCount = Beds.Length;
            SampleRate = header.SampleRate;

            // Will fail on subsequent calls as MLP is actually not supported
            return new float[0];
        }

        /// <inheritdoc/>
        public override void Seek(long sample) {
            throw new NotImplementedException();
        }
    }
}
