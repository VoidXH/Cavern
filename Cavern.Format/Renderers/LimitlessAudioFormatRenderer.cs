using System;

using Cavern.Format.Decoders;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded Limitless Audio Format stream.
    /// </summary>
    public class LimitlessAudioFormatRenderer : PCMToObjectsRenderer {
        /// <summary>
        /// Renders a decoded Limitless Audio Format stream.
        /// </summary>
        public LimitlessAudioFormatRenderer(LimitlessAudioFormatDecoder stream) : base(stream) {
            SetupObjects(stream.ChannelCount);
            for (int i = 0; i < stream.ChannelCount; i++) {
                objects[i].Position = stream.ObjectPositions[i];
            }
            objectSamples[0] = Array.Empty<float>();
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the <see cref="Renderer.objects"/>.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            base.Update(samples);
            LimitlessAudioFormatDecoder lafDecoder = (LimitlessAudioFormatDecoder)stream;
            for (int i = 0; i < objectSamples.Length; i++) {
                objects[i].Position = lafDecoder.ObjectPositions[i] * Listener.EnvironmentSize;
            }
        }
    }
}