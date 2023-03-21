using System;

using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.SpecialSources;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded E-AC-3 stream with Cavern.
    /// </summary>
    public class EnhancedAC3Renderer : Renderer {
        /// <summary>
        /// The stream is coded in the Enhanced version of AC-3.
        /// </summary>
        public bool Enhanced => ((EnhancedAC3Decoder)stream).Enhanced;

        /// <summary>
        /// The stream contained sparsely decoded data, which can't be supported, as the documentation is incorrect.
        /// </summary>
        public bool WorkedAround => ((EnhancedAC3Decoder)stream).Extensions.JOC != null
            && ((EnhancedAC3Decoder)stream).Extensions.JOC.HadSparse;

        /// <summary>
        /// Count of free-floating objects.
        /// </summary>
        public int DynamicObjects { get; }

        /// <summary>
        /// Aligned timeslot results for final output.
        /// </summary>
        readonly float[][] finalResult;

        /// <summary>
        /// Standard-aligned PCM channels.
        /// </summary>
        readonly ReferenceChannel[] inputMatrix;

        /// <summary>
        /// Actually rendered channels.
        /// </summary>
        readonly ReferenceChannel[] outputMatrix;

        /// <summary>
        /// Creates the object mix from channels.
        /// </summary>
        readonly JointObjectCodingApplier applier;

        /// <summary>
        /// Result of the last decoded block (interlaced samples).
        /// </summary>
        float[] decodedBlock;

        /// <summary>
        /// LFE channel samples from the last timeslot.
        /// </summary>
        float[] lfeTimeslot;

        /// <summary>
        /// LFE channel output.
        /// </summary>
        float[] lfeResult = new float[0];

        /// <summary>
        /// Cache for the decoded bed data.
        /// </summary>
        float[][] inputData = new float[0][];

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
            inputMatrix = ChannelPrototype.GetStandardMatrix(stream.ChannelCount);
            outputMatrix = stream.GetChannels();

            // Object-based rendering
            if (HasObjects = stream.Extensions.HasObjects) {
                ObjectAudioMetadata oamd = stream.Extensions.OAMD;
                JointObjectCoding joc = stream.Extensions.JOC;
                DynamicObjects = oamd.ObjectCount - oamd.Beds;
                finalResult = new float[joc.ObjectCount][];
                applier = new JointObjectCodingApplier(joc, stream.FrameSize);
                SetupObjects(oamd.ObjectCount);
            }
            // Channel-based rendering
            else {
                for (int channel = 0; channel < outputMatrix.Length; channel++) {
                    Source source = new StreamMasterSource(reader, channel) {
                        Position = ChannelPrototype.AlternativePositions[(int)outputMatrix[channel]] * Listener.EnvironmentSize
                    };
                    objects.Add(source);
                }
                finalResult = new float[outputMatrix.Length][];
                FinishSetup(outputMatrix.Length);
            }

            for (int i = 0, c = objects.Count; i < c; i++) {
                objects[i].Volume = .707f; // E-AC-3 + JOC has a general -3 dB gain to prevent clipping
            }
        }

        /// <summary>
        /// Get the bed channels.
        /// </summary>
        public override ReferenceChannel[] GetChannels() => ((EnhancedAC3Decoder)stream).GetChannels();

        /// <summary>
        /// Get the &quot;objects&quot; that are just static channels.
        /// </summary>
        public ReferenceChannel[] GetStaticChannels() => ((EnhancedAC3Decoder)stream).Extensions.OAMD.GetStaticChannels();

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            if (lfeResult.Length != samples) {
                timeslotResult = new float[finalResult.Length][];
                for (int obj = 0; obj < finalResult.Length; obj++) {
                    finalResult[obj] = new float[samples];
                }
                lfeTimeslot = new float[samples];
                lfeResult = new float[samples];
            }
            if (inputData.Length != stream.ChannelCount) {
                decodedBlock = new float[QuadratureMirrorFilterBank.subbands * stream.ChannelCount];
                inputData = new float[stream.ChannelCount][];
                for (int channel = 0; channel < inputData.Length; channel++) {
                    inputData[channel] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }

            int pointer = 0;
            while (pointer < samples) {
                if (timeslotPosition == 0) {
                    RenderNextTimeslot();
                }
                int next = Math.Min(samples - pointer, QuadratureMirrorFilterBank.subbands - timeslotPosition);
                for (int obj = 0; obj < finalResult.Length; obj++) {
                    for (int i = 0; i < next; i++) {
                        finalResult[obj][pointer + i] = timeslotResult[obj][timeslotPosition + i];
                    }
                }
                for (int i = 0; i < next; i++) {
                    lfeResult[pointer + i] = lfeTimeslot[timeslotPosition + i];
                }
                pointer += next;
                timeslotPosition += next;
                if (timeslotPosition == QuadratureMirrorFilterBank.subbands) {
                    timeslotPosition = 0;
                }
            }

            int lfe = ((EnhancedAC3Decoder)stream).Extensions.OAMD.GetLFEPosition();
            if (lfe != -1) {
                Array.Copy(finalResult, objectSamples, lfe);
                for (int i = 0; i < outputMatrix.Length; i++) {
                    if (outputMatrix[i] == ReferenceChannel.ScreenLFE) {
                        objectSamples[lfe] = lfeResult;
                        break;
                    }
                }
                Array.Copy(finalResult, lfe, objectSamples, lfe + 1, objectSamples.Length - lfe - 1);
            } else {
                Array.Copy(finalResult, objectSamples, objectSamples.Length);
            }
        }

        /// <summary>
        /// Render new object samples for the next timeslot
        /// </summary>
        void RenderNextTimeslot() {
            stream.DecodeBlock(decodedBlock, 0, decodedBlock.LongLength);
            WaveformUtils.InterlacedToMultichannel(decodedBlock, inputData);

            EnhancedAC3Decoder decoder = (EnhancedAC3Decoder)stream;
            // Object-based rendering
            if (HasObjects = decoder.Extensions.HasObjects) {
                decoder.Extensions.OAMD.UpdateSources(decoder.LastFetchStart / stream.ChannelCount, objects);

                float[][] sources = new float[JointObjectCodingTables.inputMatrix.Length][];
                for (int i = 0; i < sources.Length; i++) {
                    for (int j = 0; j < inputMatrix.Length; j++) {
                        if (JointObjectCodingTables.inputMatrix[i] == inputMatrix[j]) {
                            sources[i] = inputData[j];
                            break;
                        }
                    }
                }

                for (int i = 0; i < inputMatrix.Length; i++) {
                    if (inputMatrix[i] == ReferenceChannel.ScreenLFE) {
                        lfeTimeslot = inputData[i];
                    }
                }

                timeslotResult = applier.Apply(sources, decoder.Extensions.JOC);
            }
            // Channel-based rendering or fallback to it when OAMD or JOC can't be decoded correctly
            else {
                for (int i = 0; i < outputMatrix.Length; i++) {
                    timeslotResult[i] = inputData[i];
                    objects[i].Position = ChannelPrototype.AlternativePositions[(int)outputMatrix[i]] * Listener.EnvironmentSize;
                    if (ChannelPrototype.Mapping[(int)outputMatrix[i]].LFE) { // LFE is handled elsewhere
                        objects[i].Position = default;
                        timeslotResult[i].Clear();
                    }
                }
                for (int i = outputMatrix.Length; i < timeslotResult.Length; i++) {
                    objects[i].Position = default;
                    timeslotResult[i].Clear();
                }
            }
        }

        /// <summary>
        /// Free up resources created by the renderer.
        /// </summary>
        public override void Dispose() {
            applier.Dispose();
            base.Dispose();
        }
    }
}