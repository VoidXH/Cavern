using Cavern.Format.Decoders;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Maps interlaced PCM samples to Cavern objects.
    /// </summary>
    public abstract class PCMToObjectsRenderer : Renderer {
        /// <summary>
        /// Intermediate array to decode to.
        /// </summary>
        float[] decodeCache;

        /// <summary>
        /// Maps interlaced PCM samples to Cavern objects.
        /// </summary>
        /// <param name="stream"></param>
        protected PCMToObjectsRenderer(Decoder stream) : base(stream) { }

        /// <inheritdoc/>
        public override void Update(int samples) {
            if (objectSamples[0].Length != samples) {
                decodeCache = new float[samples * stream.ChannelCount];
                for (int i = 0; i < objectSamples.Length; i++) {
                    objectSamples[i] = new float[samples];
                }
            }
            stream.DecodeBlock(decodeCache, 0, decodeCache.LongLength);
            WaveformUtils.InterlacedToMultichannel(decodeCache, objectSamples);
        }
    }
}
