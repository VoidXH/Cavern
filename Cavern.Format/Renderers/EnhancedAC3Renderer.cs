using System.Numerics;

using Cavern.Format.Decoders;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Remapping;
using Cavern.SpecialSources;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded E-AC-3 stream with Cavern.
    /// </summary>
    public class EnhancedAC3Renderer : Renderer {
        /// <summary>TODO: TEMPORARY UNTIL THE DECODER IS IMPLEMENTED!</summary>
        public AudioReader Source { get; set; }

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
        /// Last non-interpolated position of each object.
        /// </summary>
        readonly Vector3[] lastHoldPos;

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
                lastHoldPos = new Vector3[oamd.ObjectCount];
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
            if (Source != null)
                Source.ReadBlock(input, 0, input.LongLength);
            float[][] grouped = WaveformUtils.InterlacedToMultichannel(input, stream.ChannelCount);

            if (hasObjects) {
                ObjectAudioMetadata oamd = null;
                JointObjectCoding joc = null;
                if (stream.Extensions != null) {
                    oamd = stream.Extensions.OAMD;
                    joc = stream.Extensions.JOC;
                }
                if (oamd == null || joc == null)
                    return;

                int offset = stream.Extensions.GetPayloadByID(ExtensibleMetadataDecoder.oamdPayloadID).SampleOffset;
                oamd.UpdateSources(stream.LastFetchStart - offset, objects, lastHoldPos);

                ReferenceChannel[] matrix = ChannelPrototype.StandardMatrix[stream.ChannelCount];
                float[][] sources = new float[stream.Channels.Length][];
                for (int i = 0; i < sources.Length; ++i) {
                    for (int j = 0; j < matrix.Length; ++j) {
                        if (stream.Channels[i] == matrix[j]) {
                            sources[i] = grouped[j];
                            break;
                        }
                    }
                }

                float[][] objSamples = joc.Decode(sources);
                int lfe = oamd.GetLFEPosition();
                if (lfe != -1) {
                    for (int obj = 0; obj < lfe; ++obj)
                        objectSamples[obj] = objSamples[obj];
                    for (int i = 0; i < matrix.Length; ++i) {
                        if (matrix[i] == ReferenceChannel.ScreenLFE) {
                            objectSamples[lfe] = grouped[i];
                            break;
                        }
                    }
                    for (int obj = lfe + 1; obj < objectSamples.Length; ++obj)
                        objectSamples[obj] = objSamples[obj - 1];
                } else
                    for (int obj = 0; obj < objectSamples.Length; ++obj)
                        objectSamples[obj] = objSamples[obj];
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