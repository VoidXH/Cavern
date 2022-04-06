using System.Collections.Generic;

using Cavern.Format.Decoders;
using Cavern.Remapping;
using Cavern.SpecialSources;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded stream with Cavern.
    /// </summary>
    public abstract partial class Renderer {
        /// <summary>
        /// Rendered spatial objects.
        /// </summary>
        public IReadOnlyList<Source> Objects => objects;

        /// <summary>
        /// Rendered spatial objects.
        /// </summary>
        protected readonly List<Source> objects = new List<Source>();

        /// <summary>
        /// Source stream.
        /// </summary>
        protected readonly Decoder stream;

        /// <summary>
        /// Sample supplier for the rendered objects.
        /// </summary>
        protected readonly StreamMaster reader;

        /// <summary>
        /// Samples for the rendered objects in the last update.
        /// </summary>
        protected float[][] objectSamples;

        /// <summary>
        /// Renders a decoded stream with Cavern.
        /// </summary>
        public Renderer(Decoder stream) {
            this.stream = stream;
            reader = new StreamMaster(GetNextObjectSamples);
        }

        /// <summary>
        /// Get the bed channels.
        /// </summary>
        public virtual ReferenceChannel[] GetChannels() => ChannelPrototype.StandardMatrix[stream.ChannelCount];

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the <see cref="objects"/>.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public abstract void Update(int samples);

        /// <summary>
        /// Update the objects and get the samples they need to render.
        /// </summary>
        public float[][] GetNextObjectSamples(int samples) {
            Update(samples);
            return objectSamples;
        }

        /// <summary>
        /// Set up the renderer for a number of objects.
        /// </summary>
        protected void SetupObjects(int count) {
            for (int obj = 0; obj < count; ++obj)
                objects.Add(new StreamMasterSource(reader, obj));
            FinishSetup(count);
        }

        /// <summary>
        /// Set up the renderer for a number of standard channels.
        /// </summary>
        protected void SetupChannels(int count) {
            ReferenceChannel[] matrix = ChannelPrototype.StandardMatrix[count];
            for (int channel = 0; channel < count; ++channel) {
                Source source = new StreamMasterSource(reader, channel) {
                    Position = channelPositions[(int)matrix[channel]] * Listener.EnvironmentSize
                };
                objects.Add(source);
            }
            FinishSetup(count);
        }

        /// <summary>
        /// Finishing steps of creating a layout.
        /// </summary>
        protected void FinishSetup(int count) {
            reader.SetupSources(objects, stream.SampleRate);
            objectSamples = new float[count][];
        }
    }
}