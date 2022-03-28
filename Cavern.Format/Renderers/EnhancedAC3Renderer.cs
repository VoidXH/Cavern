using System.Numerics;

using Cavern.Format.Decoders;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.SpecialSources;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded E-AC-3 stream with Cavern.
    /// </summary>
    public class EnhancedAC3Renderer : Renderer {
        /// <summary>
        /// The stream is object-based.
        /// </summary>
        readonly bool hasObjects;

        /// <summary>
        /// Samples for the rendered objects in the last update.
        /// </summary>
        readonly float[][] objectSamples;

        /// <summary>
        /// Source stream.
        /// </summary>
        readonly EnhancedAC3Decoder stream;

        /// <summary>
        /// Sample supplier for the rendered objects.
        /// </summary>
        readonly StreamMaster reader;

        /// <summary>
        /// Parse an E-AC-3 decoder to a renderer.
        /// </summary>
        public EnhancedAC3Renderer(EnhancedAC3Decoder stream) {
            this.stream = stream;
            ObjectAudioMetadata oamd = null;
            if (stream.Extensions != null)
                oamd = stream.Extensions.OAMD;
            hasObjects = oamd != null;
            if (hasObjects) {
                objectSamples = new float[oamd.ObjectCount][];
                reader = new StreamMaster(GetNextObjectSamples);
                for (int obj = 0; obj < oamd.ObjectCount; ++obj)
                    objects.Add(new StreamMasterSource(reader, obj));
                reader.SetupSources(objects, stream.SampleRate);
            }
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        public override void Update(int samples) {
            float[] input = new float[samples * stream.ChannelCount];
            stream.DecodeBlock(input, 0, input.LongLength);

            if (hasObjects) {
                ObjectAudioMetadata oamd = null;
                if (stream.Extensions != null)
                    oamd = stream.Extensions.OAMD;
                if (oamd == null)
                    return;

                Vector3[] positions = oamd.GetPositions(stream.LastFetchStart);
                for (int obj = 0; obj < positions.Length; ++obj) {
                    objects[obj].Position = positions[obj];
                    objectSamples[obj] = new float[samples];
                }
            }
        }

        /// <summary>
        /// Update the objects and get the samples they need to render.
        /// </summary>
        float[][] GetNextObjectSamples(int samples) {
            Update(samples);
            return objectSamples;
        }
    }
}