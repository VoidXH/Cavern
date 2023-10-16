using Cavern.Format.Decoders;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded Limitless Audio Format stream.
    /// </summary>
    public class LimitlessAudioFormatRenderer : Renderer {
        /// <summary>
        /// Intermediate array to render to.
        /// </summary>
        float[] streamCache;

        /// <summary>
        /// Renders a decoded Limitless Audio Format stream.
        /// </summary>
        public LimitlessAudioFormatRenderer(LimitlessAudioFormatDecoder stream) : base(stream) {
            SetupObjects(stream.ChannelCount);
            for (int i = 0; i < stream.ChannelCount; i++) {
                objects[i].Position = stream.ObjectPositions[i];
            }
            objectSamples[0] = new float[0];
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the <see cref="Renderer.objects"/>.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            if (objectSamples[0].Length != samples) {
                streamCache = new float[samples * stream.ChannelCount];
                for (int i = 0; i < objectSamples.Length; i++) {
                    objectSamples[i] = new float[samples];
                }
            }

            LimitlessAudioFormatDecoder lafDecoder = (LimitlessAudioFormatDecoder)stream;
            stream.DecodeBlock(streamCache, 0, streamCache.LongLength);
            for (int i = 0; i < objectSamples.Length; i++) {
                WaveformUtils.ExtractChannel(streamCache, objectSamples[i], i, objectSamples.Length);
                objects[i].Position = lafDecoder.ObjectPositions[i] * Listener.EnvironmentSize;
            }
        }
    }
}