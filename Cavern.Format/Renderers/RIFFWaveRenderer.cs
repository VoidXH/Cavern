using Cavern.Format.Decoders;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded RIFF WAVE stream.
    /// </summary>
    public class RIFFWaveRenderer : Renderer {
        /// <summary>
        /// Reused array for output rendering.
        /// </summary>
        float[] render;

        /// <summary>
        /// Renders a decoded RIFF WAVE stream.
        /// </summary>
        public RIFFWaveRenderer(RIFFWaveDecoder stream) : base(stream) {
            if (stream.ADM == null) {
                SetupChannels(stream.ChannelCount);
            } else {
                SetupObjects(stream.ChannelCount);
            }
            objectSamples[0] = new float[0];
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            if (objectSamples[0].Length != samples) {
                for (int i = 0; i < objectSamples.Length; i++)
                    objectSamples[i] = new float[samples];
                render = new float[objectSamples.Length * samples];
            }

            stream.DecodeBlock(render, 0, render.LongLength);
            WaveformUtils.InterlacedToMultichannel(render, objectSamples);
        }
    }
}