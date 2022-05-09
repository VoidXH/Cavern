using System;
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
        public bool HasObjects { get; private set; }

        /// <summary>
        /// Count of free-floating objects.
        /// </summary>
        public int DynamicObjects { get; }

        /// <summary>
        /// Aligned timeslot results for final output.
        /// </summary>
        readonly float[][] finalResult;

        /// <summary>
        /// Creates the object mix from channels.
        /// </summary>
        readonly JointObjectCodingApplier applier;

        /// <summary>
        /// Last non-interpolated position of each object.
        /// </summary>
        readonly Vector3[] lastHoldPos;

        /// <summary>
        /// LFE channel samples from the last timeslot.
        /// </summary>
        float[] lfeTimeslot;

        /// <summary>
        /// LFE channel output.
        /// </summary>
        float[] lfeResult = new float[0];

        /// <summary>
        /// Object samples for the last rendered timeslot.
        /// </summary>
        float[][] timeslotResult;

        /// <summary>
        /// Sample in the last fetched <see cref="timeslotResult"/>.
        /// </summary>
        int timeslotPosition;

        /// <summary>
        /// Parse an E-AC-3 decoder to a renderer.
        /// </summary>
        public EnhancedAC3Renderer(EnhancedAC3Decoder stream) : base(stream) {
            // Object-based rendering
            if (HasObjects = stream.Extensions.HasObjects) {
                ObjectAudioMetadata oamd = stream.Extensions.OAMD;
                JointObjectCoding joc = stream.Extensions.JOC;
                DynamicObjects = joc.ObjectCount;
                finalResult = new float[joc.ObjectCount][];
                lastHoldPos = new Vector3[oamd.ObjectCount];
                applier = new JointObjectCodingApplier(joc.ObjectCount, stream.FrameSize);
                SetupObjects(oamd.ObjectCount);
            }
            // Channel-based rendering
            else {
                ReferenceChannel[] channels = stream.GetChannels();
                for (int channel = 0; channel < channels.Length; ++channel) {
                    Source source = new StreamMasterSource(reader, channel) {
                        Position = channelPositions[(int)channels[channel]] * Listener.EnvironmentSize
                    };
                    objects.Add(source);
                }
                finalResult = new float[channels.Length][];
                FinishSetup(channels.Length);
            }
        }

        /// <summary>
        /// Get the bed channels.
        /// </summary>
        public override ReferenceChannel[] GetChannels() => ((EnhancedAC3Decoder)stream).GetChannels();

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            if (lfeResult.Length != samples) {
                timeslotResult = new float[finalResult.Length][];
                for (int obj = 0; obj < finalResult.Length; ++obj)
                    finalResult[obj] = new float[samples];
                lfeTimeslot = new float[samples];
                lfeResult = new float[samples];
            }

            int pointer = 0;
            while (pointer < samples) {
                if (timeslotPosition == 0)
                    RenderNextTimeslot();
                int next = Math.Min(samples - pointer, QuadratureMirrorFilterBank.subbands - timeslotPosition);
                for (int obj = 0; obj < finalResult.Length; ++obj)
                    for (int i = 0; i < next; ++i)
                        finalResult[obj][pointer + i] = timeslotResult[obj][timeslotPosition + i];
                for (int i = 0; i < next; ++i)
                    lfeResult[pointer + i] = lfeTimeslot[timeslotPosition + i];
                pointer += next;
                timeslotPosition += next;
                if (timeslotPosition == QuadratureMirrorFilterBank.subbands)
                    timeslotPosition = 0;
            }

            int lfe = ((EnhancedAC3Decoder)stream).Extensions.OAMD.GetLFEPosition();
            if (lfe != -1) {
                for (int obj = 0; obj < lfe; ++obj)
                    objectSamples[obj] = finalResult[obj];
                ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(stream.ChannelCount);
                for (int i = 0; i < matrix.Length; ++i) {
                    if (matrix[i] == ReferenceChannel.ScreenLFE) {
                        objectSamples[lfe] = lfeResult;
                        break;
                    }
                }
                for (int obj = lfe + 1; obj < objectSamples.Length; ++obj)
                    objectSamples[obj] = finalResult[obj - 1];
            } else
                for (int obj = 0; obj < objectSamples.Length; ++obj)
                    objectSamples[obj] = finalResult[obj];
        }

        /// <summary>
        /// Render new object samples for the next timeslot
        /// </summary>
        void RenderNextTimeslot() {
            float[] input = new float[QuadratureMirrorFilterBank.subbands * stream.ChannelCount];
            stream.DecodeBlock(input, 0, input.LongLength);
            if (Source != null)
                Source.ReadBlock(input, 0, input.LongLength);
            float[][] grouped = WaveformUtils.InterlacedToMultichannel(input, stream.ChannelCount);

            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(stream.ChannelCount);
            EnhancedAC3Decoder decoder = (EnhancedAC3Decoder)stream;
            // Object-based rendering
            if (HasObjects = decoder.Extensions.HasObjects) {
                decoder.Extensions.OAMD.UpdateSources(decoder.LastFetchStart / stream.ChannelCount, objects, lastHoldPos);

                float[][] sources = new float[JointObjectCodingTables.inputMatrix.Length][];
                for (int i = 0; i < sources.Length; ++i) {
                    for (int j = 0; j < matrix.Length; ++j) {
                        if (JointObjectCodingTables.inputMatrix[i] == matrix[j]) {
                            sources[i] = grouped[j];
                            break;
                        }
                    }
                }

                for (int i = 0; i < matrix.Length; ++i)
                    if (matrix[i] == ReferenceChannel.ScreenLFE)
                        lfeTimeslot = grouped[i];

                timeslotResult = applier.Apply(sources, decoder.Extensions.JOC);
            }
            // Channel-based rendering or fallback to it when OAMD or JOC can't be decoded correctly
            else {
                for (int i = 0; i < matrix.Length; ++i) {
                    timeslotResult[i] = grouped[i];
                    objects[i].Position = channelPositions[(int)matrix[i]] * Listener.EnvironmentSize;
                    if (ChannelPrototype.Mapping[(int)matrix[i]].LFE) { // LFE is handled elsewhere
                        objects[i].Position = default;
                        Array.Clear(timeslotResult[i], 0, timeslotResult[i].Length);
                    }
                }
                for (int i = matrix.Length; i < timeslotResult.Length; ++i) {
                    objects[i].Position = default;
                    Array.Clear(timeslotResult[i], 0, timeslotResult[i].Length);
                }
            }
        }
    }
}